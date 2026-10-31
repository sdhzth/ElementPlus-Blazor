using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTree
    {
        private TreeItemBase draggingNode;
        private TreeItemBase _currentNode;

        // ========== 原有参数 ==========

        [Parameter]
        public RenderFragment<TreeItemBase> NodeTemplate { get; set; }

        [Parameter]
        public string FilterText { get; set; }

        [Parameter]
        public Func<TreeItemBase, string, bool> FilterNodeMethod { get; set; }

        /// <summary>
        /// 在树上方渲染关键字过滤输入框，输入即调用 <see cref="Filter"/>
        /// （对齐 BootstrapBlazor TreeView 的 ShowSearch + OnSearchAsync）。
        /// </summary>
        [Parameter]
        public bool ShowSearch { get; set; }

        [Parameter]
        public string SearchPlaceholder { get; set; } = "请输入关键字";

        [Parameter]
        public bool ShowCheckbox { get; set; }

        [Parameter]
        public bool CheckStrictly { get; set; }

        [Parameter]
        public EventCallback<List<TreeItemBase>> CheckedNodesChanged { get; set; }

        [Parameter]
        public bool Lazy { get; set; }

        [Parameter]
        public Func<TreeItemBase, Task<IEnumerable<TreeItemBase>>> LazyLoad { get; set; }

        [Parameter]
        public bool Draggable { get; set; }

        [Parameter]
        public EventCallback<TreeDragEventArgs> OnNodeDrop { get; set; }

        // ========== 官方 API 补充 ==========

        [Parameter]
        public List<int> DefaultExpandedKeys { get; set; }

        [Parameter]
        public List<int> DefaultCheckedKeys { get; set; }

        [Parameter]
        public bool ExpandOnClickNode { get; set; } = true;

        [Parameter]
        public bool HighlightCurrent { get; set; } = true;

        [Parameter]
        public bool AutoExpandParent { get; set; } = true;

        [Parameter]
        public int Indent { get; set; } = 18;

        [Parameter]
        public string NodeIcon { get; set; }

        /// <summary>
        /// 空态文案。留空时取词条 <c>el.tree.emptyText</c>；不接词条时仍是本仓原来的「无数据」
        /// （EP 该键的值是「暂无数据」，所以接了词条才会变）。
        /// </summary>
        [Parameter]
        public string EmptyText { get; set; }

        /// <summary>空态最终取值：显式参数 &gt; 词条 &gt; 本仓原来的「无数据」。</summary>
        private string EffectiveEmptyText => Lang.Text("el.tree.emptyText", EmptyText, "无数据");

        [Parameter]
        public RenderFragment EmptyContent { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public string AriaLabel { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeClick { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDblClick { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeExpand { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeCollapse { get; set; }

        // ========== 官方 API 补充（第二批） ==========

        /// <summary>点击节点时是否同时勾选复选框（官方 check-on-click-node，默认 false）</summary>
        [Parameter]
        public bool CheckOnClickNode { get; set; } = false;

        /// <summary>点击叶子节点时是否勾选复选框（官方 check-on-click-leaf，默认 true）</summary>
        [Parameter]
        public bool CheckOnClickLeaf { get; set; } = true;

        /// <summary>当前选中节点 key（官方 current-node-key）</summary>
        [Parameter]
        public int? CurrentNodeKey { get; set; }

        [Parameter]
        public EventCallback<int?> CurrentNodeKeyChanged { get; set; }

        /// <summary>是否手风琴模式：同一层级最多展开一个（官方 accordion）</summary>
        [Parameter]
        public bool Accordion { get; set; } = false;

        /// <summary>是否允许拖拽该节点（官方 allow-drag）</summary>
        [Parameter]
        public Func<TreeItemBase, bool> AllowDrag { get; set; }

        /// <summary>是否允许拖放到目标节点（官方 allow-drop：dropType 为 inner/prev/next）</summary>
        [Parameter]
        public Func<TreeItemBase, TreeItemBase, string, bool> AllowDrop { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeContextMenu { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnCheck { get; set; }

        [Parameter]
        public EventCallback<List<TreeItemBase>> OnCheckChange { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnCurrentChange { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDragStart { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDragEnter { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDragLeave { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDragOver { get; set; }

        [Parameter]
        public EventCallback<TreeItemBase> OnNodeDragEnd { get; set; }

        // ========== CSS 类 ==========

        protected string TreeClass => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tree", Cls)
            .AddIf(Draggable, "is-draggable")
            .AddIf(HighlightCurrent, "el-tree--highlight-current")
            .ToString();

        protected IReadOnlyList<TreeItemBase> VisibleRootNodes
        {
            get
            {
                // 每轮渲染从根节点重新取数：DataSource 可能被宿主整个换掉，
                // 按 Id 缓存的「值节点祖先链」必须跟着作废，否则会拿旧树判勾选。
                ResetTreeSelectCheckCache();
                return GetRootNodes()
                    .Where(IsVisible)
                    .ToList();
            }
        }

        private IReadOnlyList<TreeItemBase> GetRootNodes()
        {
            if (items == null)
            {
                return new List<TreeItemBase>();
            }

            return DataFormat == DataFormat.List
                ? items.Where(x => x.ParentId == 0).ToList()
                : items;
        }

        protected RenderFragment RenderTreeNode(TreeItemBase node)
        {
            return builder => BuildTreeNode(builder, node, new HashSet<TreeItemBase>());
        }

        private RenderFragment RenderTreeNode(TreeItemBase node, HashSet<TreeItemBase> visited)
        {
            return builder => BuildTreeNode(builder, node, visited);
        }

        private void BuildTreeNode(RenderTreeBuilder builder, TreeItemBase node, HashSet<TreeItemBase> visited)
        {
            // Defensive guard: stop recursion on null nodes or cycles (same node reached
            // again as a descendant), which would otherwise cause a stack overflow.
            if (node == null || !visited.Add(node))
            {
                return;
            }

            var seq = 0;
            var children = GetChildrenForRender(node);
            var hasChildren = node.HasChildren || children.Any();
            node.Direction = hasChildren && node.Expanded ? "expanded" : string.Empty;
            var nodeClass = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-tree-node", "is-focusable")
                .AddIf(node.Expanded, "is-expanded")
                .AddIf(node.Disabled, "is-disabled")
                // EP 的 el-tree 只有一个 current-node-key ⇒ is-current 至多一个节点，
                // 勾选模式与独立树同理（多选靠 checkbox，不靠高亮堆叠）。
                .AddIf(ReferenceEquals(_currentNode, node), "is-current")
                .ToString();
            var iconClass = HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-tree-node__expand-icon", string.IsNullOrWhiteSpace(NodeIcon) ? "el-icon-caret-right" : NodeIcon, node.Direction)
                .AddIf(!hasChildren, "is-leaf")
                .ToString();

            builder.OpenElement(seq++, "div");
            builder.SetKey(node.Id);
            builder.AddAttribute(seq++, "role", "treeitem");
            builder.AddAttribute(seq++, "tabindex", node.Disabled ? "-1" : "0");
            builder.AddAttribute(seq++, "aria-disabled", node.Disabled.ToString().ToLowerInvariant());
            builder.AddAttribute(seq++, "draggable", Draggable && !node.Disabled ? "true" : "false");
            builder.AddAttribute(seq++, "class", nodeClass);
            builder.AddAttribute(seq++, "ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, _ => OnFeatureDragStart(node)));
            builder.AddAttribute(seq++, "ondragenter", EventCallback.Factory.Create<DragEventArgs>(this, _ => OnFeatureDragEnterAsync(node)));
            builder.AddAttribute(seq++, "ondragleave", EventCallback.Factory.Create<DragEventArgs>(this, _ => OnFeatureDragLeaveAsync(node)));
            builder.AddAttribute(seq++, "ondragover", EventCallback.Factory.Create<DragEventArgs>(this, OnFeatureDragOverAsync));
            builder.AddAttribute(seq++, "ondragend", EventCallback.Factory.Create<DragEventArgs>(this, _ => OnFeatureDragEndAsync(node)));
            builder.AddAttribute(seq++, "ondrop", EventCallback.Factory.Create<DragEventArgs>(this, _ => OnFeatureDropAsync(node)));
            builder.AddAttribute(seq++, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, _ =>
            {
                if (OnNodeContextMenu.HasDelegate) OnNodeContextMenu.InvokeAsync(node);
            }));

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-tree-node__content");
            builder.AddAttribute(seq++, "style", $"padding-left: {Indent * (node.Level ?? 0)}px;");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, async _ =>
            {
                // EP 的 tree-select 在 showCheckbox 时点节点文字**不写入选中值**
                // （Temp/ep2140-treeselect-tree.mjs:93-101 的 onNodeClick：只有 !showCheckbox 才 handleOptionSelect），
                // 文字点击只做「设为当前节点 + 展开」，选中值由勾选那条路（onCheck）决定。
                // 改前这里无条件写值，于是「点文字」和「勾框」两条路各写各的、勾选还只增不减 ——
                // 面板勾着两项而输入框只回显一项，就是 #111 报的「勾选状态与输入框回显不一致」。
                if (TreeSelectCheckMode)
                {
                    await NotifyFeatureNodeClickAsync(node);
                }
                else
                {
                    await SelectFeatureNodeAsync(node);
                }
                // EP tree-select 传给树的 expandOnClickNode 是 `!checkStrictly && props.expandOnClickNode`
                // （同文件 :66-68）：严格模式下点文字不该顺带折叠子树。
                if (ShouldExpandOnClickNode)
                {
                    await ToggleFeatureNodeAsync(node);
                }
                if (ShowCheckbox)
                {
                    var isLeaf = !hasChildren;
                    if ((isLeaf && CheckOnClickLeaf) || (!isLeaf && CheckOnClickNode))
                    {
                        await ToggleFeatureCheckAsync(node, IsNodeCheckedForRender(node) ? Status.UnChecked : Status.Checked);
                    }
                }
            }));
            builder.AddAttribute(seq++, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, async _ =>
            {
                if (OnNodeDblClick.HasDelegate) await OnNodeDblClick.InvokeAsync(node);
            }));

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", iconClass);
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => ToggleFeatureNodeAsync(node)));
            builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
            builder.CloseElement();

            if (ShowCheckbox)
            {
                // EP 给复选框本体挂了 `@click.stop`（Temp/ep2140-tree-tree-node.mjs:65 的 withModifiers(() => {}, ["stop"])），
                // 点框的 click 不会冒泡到 el-tree-node__content 的 handleClick。本库此前没有这层拦截：
                // 一次点击既走复选框的 StatusChanged、又走 content 的「点文字」分支，
                // 叶子节点因此被**翻两次**（勾了又取消），表现就是「点框没反应 / 勾选和回显对不上」。
                // ElCheckbox 的 label 上没有 @attributes 落地，所以只能在外面套一层带 stopPropagation 的 span。
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => { }));
                builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
                builder.OpenComponent<ElCheckbox<bool>>(seq++);
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.Status), NodeCheckStatusForRender(node));
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.IsDisabled), node.Disabled);
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.StatusChanged), EventCallback.Factory.Create<Status>(this, status => ToggleFeatureCheckAsync(node, status)));
                builder.CloseComponent();
                builder.CloseElement();
            }

            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-tree-node__label");
            if (NodeTemplate != null)
            {
                builder.AddContent(seq++, NodeTemplate(node));
            }
            else
            {
                builder.AddContent(seq++, node.Text);
            }
            builder.CloseElement();
            builder.CloseElement();

            // 子节点必须渲染在**本节点那个 div 之内**。
            // Element Plus 的整套树形样式（.el-tree-node > .el-tree-node__children 的
            // 边框 / 层级、:scope 选择器、折叠时整块消失）都建立在这层嵌套上；
            // 这里原本多关了一次 div，于是子节点组被当成父节点的**兄弟**输出：
            // 树永远是平铺的 11 行、所有节点缩进 0，点箭头虽然改了 Expanded 状态，
            // 但子节点本来就在父节点外面，画面纹丝不动 —— 表现就是「展开/收缩没反应」。
            if (node.Expanded && children.Any())
            {
                builder.OpenElement(seq++, "div");
                builder.AddAttribute(seq++, "role", "group");
                builder.AddAttribute(seq++, "class", "el-tree-node__children");
                builder.AddAttribute(seq++, "aria-expanded", "true");
                foreach (var child in children)
                {
                    builder.AddContent(seq++, RenderTreeNode(child, visited));
                }
                builder.CloseElement();
            }

            builder.CloseElement();
        }

        private List<TreeItemBase> GetChildrenForRender(TreeItemBase node)
        {
            return FindFeatureChildren(node).Where(IsVisible).ToList();
        }

        private List<TreeItemBase> FindFeatureChildren(TreeItemBase treeItem)
        {
            if (items == null || treeItem == null)
            {
                return new List<TreeItemBase>();
            }

            // Id == 0 是「根」的哨兵值（List 模式下 ParentId == 0 表示根节点），
            // 所以 Id 为 0 的节点自身就是根，不能再拿它去反查子节点 ——
            // 否则会把自己和所有兄弟根当成自己的孩子而形成自我递归，
            // 在 WASM 下就是致命且不可恢复的 StackOverflowException。
            if (DataFormat == DataFormat.List && treeItem.Id == 0)
            {
                return new List<TreeItemBase>();
            }

            return DataFormat switch
            {
                DataFormat.List => items.Where(x => x != null && x.ParentId == treeItem.Id).ToList(),
                DataFormat.Children => treeItem.Children?.Where(x => x != null).ToList() ?? new List<TreeItemBase>(),
                _ => new List<TreeItemBase>()
            };
        }

        private bool IsVisible(TreeItemBase item)
        {
            if (string.IsNullOrWhiteSpace(FilterText))
            {
                return true;
            }

            if (FilterNodeMethod != null && FilterNodeMethod(item, FilterText))
            {
                return true;
            }

            if (FilterNodeMethod == null && (item.Text ?? string.Empty).Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return FindFeatureChildren(item).Any(IsVisible);
        }

        private async Task SelectFeatureNodeAsync(TreeItemBase treeNode)
        {
            if (treeNode.Disabled)
            {
                return;
            }

            // 与 ApplyTreeSelectCheckAsync 同口径：选中集只装当前这一个节点。
            // 改前是「只增不减」，于是逐个点一遍后每个点过的节点都还带着 is-current（#员工维护部门树），
            // 宿主拿到的 SelectedNodesChanged 也在累积历史点击，而不是「当前选中」。
            selectedNodes.Clear();
            selectedNodes.Add(treeNode);

            if (SelectedNodesChanged.HasDelegate)
            {
                await SelectedNodesChanged.InvokeAsync(selectedNodes);
            }

            if (Select != null)
            {
                await Select.OnInternalSelectAsync(new SelectResultModel<int?>
                {
                    Text = treeNode.TextPath,
                    Key = treeNode.Id
                });
            }

            // 官方 current-change：当前高亮节点变化时触发
            if (!ReferenceEquals(_currentNode, treeNode))
            {
                _currentNode = treeNode;
                CurrentNodeKey = treeNode.Id;
                if (CurrentNodeKeyChanged.HasDelegate)
                {
                    await CurrentNodeKeyChanged.InvokeAsync(treeNode.Id);
                }
                if (OnCurrentChange.HasDelegate)
                {
                    await OnCurrentChange.InvokeAsync(treeNode);
                }
            }

            if (OnNodeClick.HasDelegate)
            {
                await OnNodeClick.InvokeAsync(treeNode);
            }
        }

        private async Task ToggleFeatureNodeAsync(TreeItemBase node)
        {
            if (node.Disabled)
            {
                return;
            }

            var children = FindFeatureChildren(node);
            var shouldLoadLazyChildren = Lazy && LazyLoad != null && node.HasChildren && !children.Any();
            if (node.Expanded && !shouldLoadLazyChildren)
            {
                node.Expanded = false;
                if (OnNodeCollapse.HasDelegate) await OnNodeCollapse.InvokeAsync(node);
                Refresh();
                return;
            }

            node.Expanded = true;
            if (Accordion)
            {
                var siblings = node.Parent != null
                    ? FindFeatureChildren(node.Parent)
                    : GetRootNodes().ToList();
                foreach (var sib in siblings.Where(s => !ReferenceEquals(s, node) && s.Expanded))
                {
                    sib.Expanded = false;
                }
            }
            if (OnNodeExpand.HasDelegate) await OnNodeExpand.InvokeAsync(node);
            if (shouldLoadLazyChildren)
            {
                node.IsLoading = true;
                Refresh();
                var loaded = (await LazyLoad(node))?.ToList() ?? new List<TreeItemBase>();
                node.IsLoading = false;
                MergeFeatureChildren(node, loaded);
                InitFeatureLevels();
            }

            Refresh();
        }

        private void MergeFeatureChildren(TreeItemBase parent, IList<TreeItemBase> children)
        {
            if (children == null || children.Count == 0)
            {
                parent.HasChildren = false;
                return;
            }

            foreach (var child in children)
            {
                child.ParentId = parent.Id;
                child.Parent = parent;
            }

            if (DataFormat == DataFormat.Children)
            {
                parent.Children = children.ToList();
            }
            else
            {
                items ??= new List<TreeItemBase>();
                items.RemoveAll(x => children.Any(y => y.Id == x.Id));
                items.AddRange(children);
                DataSource = items;
            }
        }

        private void InitFeatureLevels()
        {
            foreach (var root in GetRootNodes())
            {
                InitFeatureLevel(root, null);
            }
        }

        private void InitFeatureLevel(TreeItemBase node, TreeItemBase parent, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                // 数据源成环或重复引用：跳过以免无限递归
                return;
            }

            node.Level = (parent?.Level ?? -1) + 1;
            node.TextPaths = parent?.TextPaths == null ? new Dictionary<int, string>() : new Dictionary<int, string>(parent.TextPaths);
            if (!node.TextPaths.ContainsKey(node.Id))
            {
                node.TextPaths.Add(node.Id, node.Text);
            }

            foreach (var child in FindFeatureChildren(node))
            {
                InitFeatureLevel(child, node, visited);
            }
        }

        private async Task ToggleFeatureCheckAsync(TreeItemBase node, Status status)
        {
            var isChecked = status == Status.Checked;
            if (TreeSelectCheckMode)
            {
                // EP 的 onCheck（Temp/ep2140-treeselect-tree.mjs:103-131）。本库 ElTreeSelect 恒为单选，
                // 所以只需那两条分支：
                //   · checkStrictly —— 勾上取自身 Id、取消置空（`checkedKeys.includes(dataValue) ? dataValue : undefined`）；
                //   · 非严格 —— 勾一棵子树折算成「该子树的第一个叶子」；若那个叶子已等于当前值、
                //     或当前值本就落在这棵子树里，则**取消选中**（EP 用同一个表达式覆盖这两种情况）。
                // 值一写，面板的勾选集合立刻跟着值同步（见 IsNodeCheckedForRender），两边不会再各说各话。
                await ApplyTreeSelectCheckAsync(node, ResolveTreeSelectCheckValue(node, isChecked));
                Refresh();
                return;
            }

            SetFeatureChecked(node, isChecked);
            if (CheckedNodesChanged.HasDelegate)
            {
                await CheckedNodesChanged.InvokeAsync(GetCheckedNodes());
            }

            Refresh();
        }

        // ========== 树选择器（ElTreeSelect）+ 复选框：勾选状态以 Select.Value 为唯一真相 ==========
        //
        // EP 的 tree-select 从不让树自己攒勾选集合：`watch([modelValue, tree])` 会把
        // `setCheckedKeys(toValidArray(modelValue))` 下发给树，onCheck 里更新完 modelValue 之后
        // 再 `nextTick(() => tree.setCheckedKeys(toValidArray(modelValue)))` 收一次口
        // （Temp/ep2140-treeselect-tree.mjs:11-19、121-123）。本库改前缺的正是这次收口：
        // 每勾一个节点只是把 node.Checked 置真、从没清除上一个，而输入框只能显示单值
        // ⇒「面板勾着 A-1 和 B-2、输入框只写着 B-2」。
        //
        // 本模式**不写 node.Checked**：DataSource 是宿主对象，一个页面里多个树选择器常共用同一份
        // （demo 的 /treeselect 五个框就共用一棵树），写它会把状态串到别的框上。
        // 独立 ElTree（Select == null）与普通（未开复选框的）树选择器完全不受影响。

        /// <summary>「树选择器 + 复选框」模式判定：只有它才走 value 驱动的勾选口径。</summary>
        private bool TreeSelectCheckMode => Select != null && ShowCheckbox;

        /// <summary>EP：tree-select 下发给树的 expandOnClickNode 是 <c>!checkStrictly &amp;&amp; props.expandOnClickNode</c>（同文件 :66-68）。</summary>
        private bool ShouldExpandOnClickNode => TreeSelectCheckMode ? !CheckStrictly && ExpandOnClickNode : ExpandOnClickNode;

        private HashSet<int> treeSelectAncestorCacheKeys;
        private int? treeSelectAncestorCacheKey;

        /// <summary>
        /// 当前值节点的祖先 Id 集合（含自身）。每个渲染轮次只按 Id 查一次整树，
        /// 免得逐节点递归变成 O(n²)（大树上点一下要渲几百个节点，这尺寸是会被量到的）。
        /// </summary>
        private HashSet<int> TreeSelectValueAncestorIds()
        {
            var key = Select?.Value;
            if (!key.HasValue)
            {
                return new HashSet<int>();
            }

            if (treeSelectAncestorCacheKey != key.Value || treeSelectAncestorCacheKeys == null)
            {
                var valueNode = FindFeatureNode(key.Value);
                // TextPaths 里存的是「祖先 Id → 文本」再加自身，正好是我们要的那条链。
                treeSelectAncestorCacheKeys = valueNode?.TextPaths == null
                    ? new HashSet<int>()
                    : new HashSet<int>(valueNode.TextPaths.Keys);
                treeSelectAncestorCacheKey = key.Value;
            }

            return treeSelectAncestorCacheKeys;
        }

        /// <summary>渲染一轮开始时作废祖先缓存（DataSource 可能被换掉，Id 链要重算）。</summary>
        private void ResetTreeSelectCheckCache()
        {
            treeSelectAncestorCacheKey = null;
            treeSelectAncestorCacheKeys = null;
        }

        private bool IsNodeCheckedForRender(TreeItemBase node)
        {
            if (!TreeSelectCheckMode)
            {
                return node.Checked;
            }

            var key = Select.Value;
            if (node == null || !key.HasValue)
            {
                return false;
            }

            if (key.Value == node.Id)
            {
                return true;
            }

            if (CheckStrictly)
            {
                return false;
            }

            // EP：setCheckedKeys 走的是 node.setChecked(true, deep=true)，值的**整棵子树**都算选中。
            // node.TextPaths 存的是「本节点的祖先 Id → 文本」再加自身，值 Id 在里面 ⇔ 本节点在值的子树里。
            if (node.TextPaths != null && node.TextPaths.ContainsKey(key.Value))
            {
                return true;
            }

            // EP reInitChecked：子节点「全部选中」时父节点自己转为选中（不是半选），然后继续向上递归
            // （Temp/ep2140-tree-model-node.mjs:37-40、:45-46）。
            // 只有沿独子链向上才可能成立（有一个兄弟没被勾就不成立）。
            return TreeSelectValueAncestorIds().Contains(node.Id) && OnlyChildChainReachesKey(node, key.Value);
        }

        /// <summary>沿「唯一子节点」一路向下能否走到 key —— 即 EP 里父节点转为全选中的那条折算链。</summary>
        private bool OnlyChildChainReachesKey(TreeItemBase node, int key)
        {
            var visited = new HashSet<TreeItemBase>();
            var current = node;
            while (current != null && visited.Add(current))
            {
                if (current.Id == key)
                {
                    return true;
                }

                var children = FindFeatureChildren(current).Where(x => x != null).ToList();
                if (children.Count != 1)
                {
                    return false;
                }

                current = children[0];
            }

            return false;
        }

        /// <summary>EP 的 node.indeterminate：本节点是值节点的祖先、自己又没被折算成选中 ⇒ 半选。</summary>
        private bool IsNodeHalfCheckedForRender(TreeItemBase node)
        {
            if (!TreeSelectCheckMode || CheckStrictly || node == null)
            {
                return false;
            }

            var key = Select.Value;
            if (!key.HasValue || key.Value == node.Id)
            {
                return false;
            }

            return TreeSelectValueAncestorIds().Contains(node.Id) && !IsNodeCheckedForRender(node);
        }

        private Status NodeCheckStatusForRender(TreeItemBase node)
        {
            if (IsNodeCheckedForRender(node))
            {
                return Status.Checked;
            }

            return IsNodeHalfCheckedForRender(node) ? Status.Indeterminate : Status.UnChecked;
        }

        /// <summary>整棵树深度优先展开（Children 格式的子节点不在 <c>items</c> 里，所以必须递归）。</summary>
        private List<TreeItemBase> TreeSelectAllNodes()
        {
            var visited = new HashSet<TreeItemBase>();
            var result = new List<TreeItemBase>();
            foreach (var root in GetRootNodes() ?? Enumerable.Empty<TreeItemBase>())
            {
                Collect(root);
            }

            return result;

            void Collect(TreeItemBase current)
            {
                if (current == null || !visited.Add(current))
                {
                    // 数据源成环或重复引用：跳过以免无限递归
                    return;
                }

                result.Add(current);
                foreach (var child in FindFeatureChildren(current).Where(x => x != null))
                {
                    Collect(child);
                }
            }
        }

        /// <summary>EP：勾选集合 = 「值 + 值的整棵子树」（非严格）或只有值本身（严格）。</summary>
        private List<TreeItemBase> TreeSelectCheckedNodes()
        {
            return TreeSelectAllNodes().Where(IsNodeCheckedForRender).ToList();
        }

        /// <summary>按 Id 全树查节点。Children 格式的子节点不在 <c>items</c> 里，所以必须递归。</summary>
        private TreeItemBase FindFeatureNode(int id)
        {
            var visited = new HashSet<TreeItemBase>();
            return Search(GetRootNodes() ?? Enumerable.Empty<TreeItemBase>());

            TreeItemBase Search(IEnumerable<TreeItemBase> nodes)
            {
                foreach (var node in nodes ?? Enumerable.Empty<TreeItemBase>())
                {
                    if (node == null || !visited.Add(node))
                    {
                        continue;
                    }

                    if (node.Id == id)
                    {
                        return node;
                    }

                    var hit = Search(FindFeatureChildren(node));
                    if (hit != null)
                    {
                        return hit;
                    }
                }

                return null;
            }
        }

        /// <summary>EP 的 <c>treeFind([data], d =&gt; 无子节点 &amp;&amp; !disabled)</c>：取该子树第一个可用叶子。</summary>
        private int? FirstEnabledLeafId(TreeItemBase node)
        {
            var visited = new HashSet<TreeItemBase>();
            return Search(node);

            int? Search(TreeItemBase current)
            {
                if (current == null || !visited.Add(current))
                {
                    return null;
                }

                var children = FindFeatureChildren(current).Where(x => x != null).ToList();
                if (children.Count == 0)
                {
                    return current.Disabled ? (int?)null : current.Id;
                }

                foreach (var child in children)
                {
                    var hit = Search(child);
                    if (hit.HasValue)
                    {
                        return hit;
                    }
                }

                return null;
            }
        }

        private int? ResolveTreeSelectCheckValue(TreeItemBase node, bool isChecked)
        {
            var current = Select.Value;
            if (CheckStrictly)
            {
                return isChecked ? node.Id : (int?)null;
            }

            var firstLeaf = FirstEnabledLeafId(node);
            var currentInsideSubtree = current.HasValue
                && (current.Value == node.Id
                    || (FindFeatureNode(current.Value)?.TextPaths?.ContainsKey(node.Id) ?? false));

            return firstLeaf == null || firstLeaf == current || currentInsideSubtree ? (int?)null : firstLeaf;
        }

        private async Task ApplyTreeSelectCheckAsync(TreeItemBase clicked, int? key)
        {
            var node = key.HasValue ? FindFeatureNode(key.Value) : null;

            // 宿主若订阅 SelectedNodesChanged / CheckedNodesChanged，拿到的必须是同一个真相：值节点。
            selectedNodes.Clear();
            if (node != null)
            {
                selectedNodes.Add(node);
            }

            if (SelectedNodesChanged.HasDelegate)
            {
                await SelectedNodesChanged.InvokeAsync(selectedNodes);
            }

            if (CheckedNodesChanged.HasDelegate)
            {
                await CheckedNodesChanged.InvokeAsync(TreeSelectCheckedNodes());
            }

            if (OnCheck.HasDelegate)
            {
                await OnCheck.InvokeAsync(clicked);
            }

            if (OnCheckChange.HasDelegate)
            {
                await OnCheckChange.InvokeAsync(TreeSelectCheckedNodes());
            }

            await Select.OnInternalSelectAsync(new SelectResultModel<int?>
            {
                Text = node?.TextPath,
                Key = key
            });
        }

        /// <summary>EP 的 tree-select 在 showCheckbox 时点节点文字不写值（同文件 :93-101），只设当前节点并派发事件。</summary>
        private async Task NotifyFeatureNodeClickAsync(TreeItemBase treeNode)
        {
            if (!ReferenceEquals(_currentNode, treeNode))
            {
                _currentNode = treeNode;
                CurrentNodeKey = treeNode.Id;
                if (CurrentNodeKeyChanged.HasDelegate)
                {
                    await CurrentNodeKeyChanged.InvokeAsync(treeNode.Id);
                }
                if (OnCurrentChange.HasDelegate)
                {
                    await OnCurrentChange.InvokeAsync(treeNode);
                }
            }

            if (OnNodeClick.HasDelegate)
            {
                await OnNodeClick.InvokeAsync(treeNode);
            }
        }


        private void SetFeatureChecked(TreeItemBase node, bool isChecked, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                // 数据源成环或重复引用：跳过以免无限递归
                return;
            }

            node.Checked = isChecked;
            if (CheckStrictly)
            {
                return;
            }

            foreach (var child in FindFeatureChildren(node))
            {
                SetFeatureChecked(child, isChecked, visited);
            }
        }

        public List<TreeItemBase> GetCheckedNodes()
        {
            // 树选择器的勾选模式里 node.Checked 从不下笔（见上方注释），口径是 Select.Value；
            // 独立 ElTree 保持读 node.Checked 的原口径。
            if (TreeSelectCheckMode)
            {
                return TreeSelectCheckedNodes();
            }

            return (items ?? new List<TreeItemBase>()).Where(x => x.Checked).ToList();
        }

        private void OnFeatureDragStart(TreeItemBase node)
        {
            if (!Draggable || node.Disabled)
            {
                return;
            }

            if (AllowDrag != null && !AllowDrag(node))
            {
                return;
            }

            draggingNode = node;
            if (OnNodeDragStart.HasDelegate) OnNodeDragStart.InvokeAsync(node);
        }

        private Task OnFeatureDragOverAsync(DragEventArgs e)
        {
            if (OnNodeDragOver.HasDelegate && draggingNode != null) OnNodeDragOver.InvokeAsync(draggingNode);
            return Task.CompletedTask;
        }

        private void OnFeatureDragEnterAsync(TreeItemBase node)
        {
            if (OnNodeDragEnter.HasDelegate && draggingNode != null) OnNodeDragEnter.InvokeAsync(node);
        }

        private void OnFeatureDragLeaveAsync(TreeItemBase node)
        {
            if (OnNodeDragLeave.HasDelegate && draggingNode != null) OnNodeDragLeave.InvokeAsync(node);
        }

        private void OnFeatureDragEndAsync(TreeItemBase node)
        {
            if (OnNodeDragEnd.HasDelegate) OnNodeDragEnd.InvokeAsync(node);
            draggingNode = null;
        }

        private async Task OnFeatureDropAsync(TreeItemBase dropNode)
        {
            if (!Draggable || draggingNode == null || dropNode == null || draggingNode == dropNode)
            {
                return;
            }

            if (AllowDrop != null && !AllowDrop(draggingNode, dropNode, "inner"))
            {
                draggingNode = null;
                return;
            }

            draggingNode.ParentId = dropNode.ParentId;
            if (OnNodeDrop.HasDelegate)
            {
                await OnNodeDrop.InvokeAsync(new TreeDragEventArgs
                {
                    DraggingNode = draggingNode,
                    DropNode = dropNode
                });
            }

            draggingNode = null;
            Refresh();
        }

        #region 官方 Exposes 方法

        /// <summary>按关键字过滤节点（官方 filter 方法）</summary>
        public void Filter(string text)
        {
            FilterText = text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                ExpandAncestorsOfMatches();
            }
            Refresh();
        }

        private string searchText;

        private void OnSearchTextChanged(string text)
        {
            searchText = text;
            Filter(text ?? string.Empty);
        }

        /// <summary>
        /// 过滤时自动展开「命中节点的祖先链」。
        /// 本组件的子节点只在父节点 Expanded 时才渲染（见 BuildTreeNode），
        /// 所以关键字命中了深层节点却不展开祖先时，画面上什么都看不到。
        /// </summary>
        private void ExpandAncestorsOfMatches()
        {
            if (items == null)
            {
                return;
            }

            var visited = new HashSet<TreeItemBase>();
            foreach (var root in GetRootNodes())
            {
                ExpandIfSubtreeMatches(root, visited);
            }
        }

        private bool ExpandIfSubtreeMatches(TreeItemBase node, HashSet<TreeItemBase> visited)
        {
            // 环 / 重复引用防护：与 BuildTreeNode 同款，避免无限递归（WASM 下栈溢出不可恢复）
            if (node == null || !visited.Add(node))
            {
                return false;
            }

            if (!IsVisible(node))
            {
                return false;
            }

            foreach (var child in FindFeatureChildren(node))
            {
                if (ExpandIfSubtreeMatches(child, visited))
                {
                    node.Expanded = true;
                }
            }

            return true;
        }

        /// <summary>勾选指定 key 集合的节点（官方 setCheckedKeys）</summary>
        public void SetCheckedKeys(IEnumerable<int> keys)
        {
            if (items == null) return;
            var set = new HashSet<int>(keys ?? Enumerable.Empty<int>());
            foreach (var node in items)
            {
                node.Checked = set.Contains(node.Id);
            }
            if (CheckedNodesChanged.HasDelegate) CheckedNodesChanged.InvokeAsync(GetCheckedNodes());
            if (OnCheckChange.HasDelegate) OnCheckChange.InvokeAsync(GetCheckedNodes());
            Refresh();
        }

        /// <summary>返回所有勾选节点的 key（官方 getCheckedKeys）</summary>
        public List<int> GetCheckedKeys()
        {
            return GetCheckedNodes().Select(x => x.Id).ToList();
        }

        /// <summary>勾选 / 取消勾选单个节点（官方 setChecked）</summary>
        public void SetChecked(int key, bool? checkedState = true)
        {
            var node = items?.FirstOrDefault(x => x.Id == key);
            if (node == null) return;
            SetFeatureChecked(node, checkedState ?? false);
            if (CheckedNodesChanged.HasDelegate) CheckedNodesChanged.InvokeAsync(GetCheckedNodes());
            if (OnCheckChange.HasDelegate) OnCheckChange.InvokeAsync(GetCheckedNodes());
            Refresh();
        }

        /// <summary>勾选指定节点集合（官方 setCheckedNodes）</summary>
        public void SetCheckedNodes(IEnumerable<TreeItemBase> nodes)
        {
            SetCheckedKeys((nodes ?? Enumerable.Empty<TreeItemBase>()).Select(x => x.Id));
        }

        /// <summary>返回所有「半选」节点（官方 getHalfCheckedNodes）：有子节点且选中/未选子节点并非全同</summary>
        public List<TreeItemBase> GetHalfCheckedNodes()
        {
            if (TreeSelectCheckMode)
            {
                return TreeSelectAllNodes().Where(IsNodeHalfCheckedForRender).ToList();
            }

            if (items == null) return new List<TreeItemBase>();
            return items.Where(IsHalfChecked).ToList();
        }

        /// <summary>返回所有「半选」节点的 key（官方 getHalfCheckedKeys）</summary>
        public List<int> GetHalfCheckedKeys()
        {
            return GetHalfCheckedNodes().Select(x => x.Id).ToList();
        }

        private bool IsHalfChecked(TreeItemBase node)
        {
            var descendants = GetAllDescendants(node);
            if (descendants.Count == 0) return false;
            var checkedCount = descendants.Count(x => x.Checked);
            return checkedCount > 0 && checkedCount < descendants.Count;
        }

        private List<TreeItemBase> GetAllDescendants(TreeItemBase node)
        {
            var result = new List<TreeItemBase>();
            Collect(GetFeatureChildren(node), result);
            return result;

            void Collect(IEnumerable<TreeItemBase> nodes, List<TreeItemBase> acc)
            {
                foreach (var n in nodes)
                {
                    acc.Add(n);
                    Collect(GetFeatureChildren(n), acc);
                }
            }
        }

        private List<TreeItemBase> GetFeatureChildren(TreeItemBase node)
        {
            return DataFormat == DataFormat.List
                ? (items ?? new List<TreeItemBase>()).Where(x => x.ParentId == node.Id).ToList()
                : (node.Children ?? new List<TreeItemBase>());
        }

        /// <summary>返回当前高亮节点 key（官方 getCurrentKey）</summary>
        public int? GetCurrentKey()
        {
            return _currentNode?.Id;
        }

        /// <summary>返回当前高亮节点（官方 getCurrentNode）</summary>
        public TreeItemBase GetCurrentNode()
        {
            return _currentNode;
        }

        /// <summary>设置当前高亮节点（官方 setCurrentKey）</summary>
        public void SetCurrentKey(int? key)
        {
            CurrentNodeKey = key;
            if (CurrentNodeKeyChanged.HasDelegate) CurrentNodeKeyChanged.InvokeAsync(key);
            _currentNode = key == null ? null : (items ?? new List<TreeItemBase>()).FirstOrDefault(x => x.Id == key.Value);
            Refresh();
        }

        /// <summary>按 key 获取节点（官方 getNode）</summary>
        public TreeItemBase GetNode(int key)
        {
            return (items ?? new List<TreeItemBase>()).FirstOrDefault(x => x.Id == key);
        }

        /// <summary>展开指定节点（与 TreeV2 expandNode 对齐，便于调用方直接控制）</summary>
        public void ExpandNode(int key)
        {
            var node = GetNode(key);
            if (node == null) return;
            node.Expanded = true;
            if (Accordion)
            {
                var siblings = node.Parent != null
                    ? FindFeatureChildren(node.Parent)
                    : GetRootNodes().ToList();
                foreach (var sib in siblings.Where(s => !ReferenceEquals(s, node) && s.Expanded))
                {
                    sib.Expanded = false;
                }
            }
            if (OnNodeExpand.HasDelegate) OnNodeExpand.InvokeAsync(node);
            Refresh();
        }

        /// <summary>收起指定节点（与 TreeV2 collapseNode 对齐）</summary>
        public void CollapseNode(int key)
        {
            var node = GetNode(key);
            if (node == null) return;
            node.Expanded = false;
            if (OnNodeCollapse.HasDelegate) OnNodeCollapse.InvokeAsync(node);
            Refresh();
        }

        /// <summary>追加子节点（官方 append）</summary>
        public void Append(TreeItemBase node, int? parentId)
        {
            if (node == null || items == null) return;
            node.ParentId = parentId ?? 0;
            node.Level = (parentId == null ? -1 : (GetNode(parentId.Value)?.Level ?? -1)) + 1;
            items.Add(node);
            DataSource = items;
            if (parentId != null)
            {
                var parent = GetNode(parentId.Value);
                if (parent != null) parent.Expanded = true;
            }
            Refresh();
        }

        /// <summary>移除节点及其子树（官方 remove）</summary>
        public void Remove(TreeItemBase node)
        {
            if (node == null || items == null) return;
            var toRemove = new HashSet<TreeItemBase> { node };
            CollectSubtree(node, toRemove);
            items.RemoveAll(x => toRemove.Contains(x));
            DataSource = items;
            Refresh();
        }

        /// <summary>在参照节点之前插入（官方 insertBefore）</summary>
        public void InsertBefore(TreeItemBase reference, TreeItemBase newNode)
        {
            InsertRelative(reference, newNode, before: true);
        }

        /// <summary>在参照节点之后插入（官方 insertAfter）</summary>
        public void InsertAfter(TreeItemBase reference, TreeItemBase newNode)
        {
            InsertRelative(reference, newNode, before: false);
        }

        private void InsertRelative(TreeItemBase reference, TreeItemBase newNode, bool before)
        {
            if (reference == null || newNode == null || items == null) return;
            newNode.ParentId = reference.ParentId;
            newNode.Level = reference.Level;
            var idx = items.FindIndex(x => x.Id == reference.Id);
            if (idx < 0) { items.Add(newNode); }
            else { items.Insert(before ? idx : idx + 1, newNode); }
            DataSource = items;
            Refresh();
        }

        private void CollectSubtree(TreeItemBase node, HashSet<TreeItemBase> acc)
        {
            foreach (var child in GetFeatureChildren(node))
            {
                acc.Add(child);
                CollectSubtree(child, acc);
            }
        }

        #endregion

        public override void Refresh()
        {
            StateHasChanged();
        }
    }
}