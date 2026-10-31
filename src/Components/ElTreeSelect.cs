using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    public class ElTreeSelect : ElSelect<int?>
    {
        private bool hasExplicitChildContent;
        private bool hasExplicitLabel;
        private RenderFragment explicitChildContent;

        [Parameter]
        public bool? LoadFullTree { get; set; }

        [Parameter]
        public EventCallback<List<TreeItemBase>> SelectedNodesChanged { get; set; }

        [Parameter]
        public string Url { get; set; }

        [Parameter]
        public Type ItemType { get; set; } = typeof(TreeItemBase);

        [Parameter]
        public DataFormat DataFormat { get; set; } = DataFormat.List;

        [Parameter]
        public bool AutoExpandAll { get; set; } = true;

        [Parameter]
        public List<TreeItemBase> DataSource { get; set; }

        [Parameter]
        public List<TreeItemBase> Data
        {
            get => DataSource;
            set => DataSource = value;
        }

        [Parameter]
        public RenderFragment TreeContent { get; set; }

        /// <summary>显示 checkbox</summary>
        [Parameter]
        public bool ShowCheckbox { get; set; }

        /// <summary>父子节点选中状态不再关联</summary>
        [Parameter]
        public bool CheckStrictly { get; set; }

        /// <summary>点击节点文字时是否同时展开/收缩该节点（官方 expandOnClickNode，EP 默认 true）。</summary>
        /// <remarks>
        /// EP 的 treeProps 里 <c>expandOnClickNode</c> 默认值为 true（<c>Temp/ep2140-tree-props.mjs:18-21</c>），
        /// tree-select 直接把 treeProps 透传给内嵌树，所以此前本属性默认 false 是与官方相反的：
        /// 勾选模式下点父节点文字既不写值、又不展开，父节点形同死键。
        /// 实际下发时 EP 还会再与 <c>!checkStrictly</c> 取与（<c>Temp/ep2140-treeselect-tree.mjs:66-68</c>），
        /// 那一层在 <see cref="ElTree.ShouldExpandOnClickNode"/> 里实现。
        /// </remarks>
        [Parameter]
        public bool ExpandOnClickNode { get; set; } = true;

        /// <summary>是否懒加载</summary>
        [Parameter]
        public bool Lazy { get; set; }

        /// <summary>懒加载委托</summary>
        [Parameter]
        public Func<TreeItemBase, Task<IEnumerable<TreeItemBase>>> LazyLoad { get; set; }

        /// <summary>节点自定义模板</summary>
        [Parameter]
        public RenderFragment<TreeItemBase> NodeTemplate { get; set; }

        /// <summary>空数据文字</summary>
        [Parameter]
        public string EmptyText { get; set; } = "无数据";

        /// <summary>
        /// 自定义节点过滤方法（对齐官方 filter-node-method）。参数依次为「节点」与「当前关键字」，
        /// 返回 true 表示命中。为 null 时按 <see cref="TreeItemBase.Text"/> 做不区分大小写的包含匹配。
        /// </summary>
        [Parameter]
        public Func<TreeItemBase, string, bool> FilterNodeMethod { get; set; }

        /// <summary>内嵌 ElTree 的实例引用：用于把触发器输入的关键字实时下发下去。</summary>
        private ElTree treeRef;

        /// <summary>
        /// 勾选模式下选中一项后**不收起面板**：官方 tree-select 的 onCheck 只写 modelValue，
        /// 面板留在原地好让用户连续勾选（<c>Temp/ep2140-treeselect-tree.mjs:103-131</c>），
        /// 非勾选模式仍走单选「点一项即关闭」的口径。
        /// </summary>
        protected internal override bool CloseDropDownOnSelect => !(Multiple || ShowCheckbox);

        public override Task SetParametersAsync(ParameterView parameters)
        {
            isTree = true;
            hasExplicitChildContent = parameters.TryGetValue<RenderFragment>(nameof(ChildContent), out explicitChildContent);
            hasExplicitLabel = parameters.TryGetValue<string>(nameof(Label), out _);
            return base.SetParametersAsync(parameters);
        }

        protected override void OnParametersSet()
        {
            isTree = true;
            if (TreeContent != null || !hasExplicitChildContent)
            {
                ChildContent = BuildTreeContent(TreeContent);
            }
            else
            {
                ChildContent = explicitChildContent;
            }

            PrepareTreeData();
            base.OnParametersSet();
            SyncLabelFromValue();
        }

        private RenderFragment BuildTreeContent(RenderFragment childContent)
        {
            return builder =>
            {
                var seq = 0;
                builder.OpenComponent<ElTree>(seq++);
                builder.AddAttribute(seq++, nameof(ElTree.LoadFullTree), LoadFullTree);
                builder.AddAttribute(seq++, nameof(ElTree.SelectedNodesChanged), SelectedNodesChanged);
                builder.AddAttribute(seq++, nameof(ElTree.Url), Url);
                builder.AddAttribute(seq++, nameof(ElTree.ItemType), ItemType ?? typeof(TreeItemBase));
                builder.AddAttribute(seq++, nameof(ElTree.DataFormat), DataFormat);
                builder.AddAttribute(seq++, nameof(ElTree.AutoExpandAll), AutoExpandAll);
                builder.AddAttribute(seq++, nameof(ElTree.DataSource), DataSource);
                builder.AddAttribute(seq++, nameof(ElTree.ShowCheckbox), ShowCheckbox);
                builder.AddAttribute(seq++, nameof(ElTree.CheckStrictly), CheckStrictly);
                builder.AddAttribute(seq++, nameof(ElTree.ExpandOnClickNode), ExpandOnClickNode);
                builder.AddAttribute(seq++, nameof(ElTree.Lazy), Lazy);
                builder.AddAttribute(seq++, nameof(ElTree.LazyLoad), LazyLoad);
                builder.AddAttribute(seq++, nameof(ElTree.NodeTemplate), NodeTemplate);
                builder.AddAttribute(seq++, nameof(ElTree.EmptyText), EmptyText);
                builder.AddAttribute(seq++, nameof(ElTree.FilterText), filterText);
                builder.AddAttribute(seq++, nameof(ElTree.FilterNodeMethod), FilterNodeMethod);
                // 🔥 声明式树内容（TreeContent）必须在 AddComponentReferenceCapture **之前**下发：
                // RenderTreeBuilder 只允许「OpenComponent 之后紧接一串 AddAttribute」，一旦插入
                // ComponentReferenceCapture 帧再 AddAttribute 就抛
                // InvalidOperationException: Attributes may only be added immediately after frames of
                // type Element or Component（只有 TreeContent != null 时才走到，故仅声明式用法崩，
                // Data 绑定的用法正常）。
                if (childContent != null)
                {
                    builder.AddAttribute(seq++, nameof(ElTree.ChildContent), childContent);
                }
                // 捕获内嵌树的实例：官方 tree-select 的 filterable 必须真的把关键字喂给树，
                // 否则输入框能打字、树却一条都不会过滤（此前就是这个状态）。
                builder.AddComponentReferenceCapture(seq++, captured =>
                {
                    treeRef = captured as ElTree;
                    if (treeRef != null && !string.IsNullOrWhiteSpace(filterText))
                    {
                        // 树实例比关键字晚诞生（关键字可能是打开下拉前就有的）时补一次过滤
                        treeRef.Filter(filterText);
                    }
                });
                builder.CloseComponent();
            };
        }

        /// <inheritdoc />
        protected override void OnFilterTextChanged(string value)
        {
            base.OnFilterTextChanged(value);
            if (!Filterable)
            {
                return;
            }

            // 关键字不仅来自键盘输入，也会在选中 / 清空 / 关闭下拉时被基类置空，
            // 统一在这里同步，避免「关了再开还留着上次的关键字」。
            treeRef?.Filter(value);
        }

        private void SyncLabelFromValue()
        {
            if (hasExplicitLabel || DataSource == null)
            {
                return;
            }

            if (!Value.HasValue)
            {
                Label = string.Empty;
                return;
            }

            var textPath = FindTextPath(Value.Value);
            Label = textPath ?? string.Empty;
        }

        private string FindTextPath(int id)
        {
            return DataFormat == DataFormat.Children
                ? FindTextPathInChildren(DataSource, id)
                : FindTextPathInList(id);
        }

        private string FindTextPathInList(int id)
        {
            var lookup = FlattenTreeItems(DataSource)
                .GroupBy(x => x.Id)
                .ToDictionary(x => x.Key, x => x.First());
            if (!lookup.TryGetValue(id, out var node))
            {
                return null;
            }

            var labels = new Stack<string>();
            // 向上追溯父级链路时必须防环：若某个节点的 ParentId 指向自己（或形成环），
            // 这个 while 会永不终止并无限 Push —— 结果是卡死 + 内存耗尽。
            var walked = new HashSet<TreeItemBase>();
            while (node != null && walked.Add(node))
            {
                labels.Push(node.Text);
                if (node.ParentId == 0 || !lookup.TryGetValue(node.ParentId, out node))
                {
                    node = null;
                }
            }

            return string.Join(" > ", labels.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static string FindTextPathInChildren(IEnumerable<TreeItemBase> nodes, int id, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            foreach (var node in nodes ?? Enumerable.Empty<TreeItemBase>())
            {
                if (node == null || !visited.Add(node))
                {
                    // 跳过 null 与重复 / 成环的节点，避免无限递归同时也避免 NRE
                    continue;
                }

                if (node.Id == id)
                {
                    return node.Text;
                }

                var childPath = FindTextPathInChildren(node.Children, id, visited);
                if (!string.IsNullOrWhiteSpace(childPath))
                {
                    return string.IsNullOrWhiteSpace(node.Text) ? childPath : $"{node.Text} > {childPath}";
                }
            }

            return null;
        }

        private void PrepareTreeData()
        {
            if (DataSource == null)
            {
                return;
            }

            // 环 / 重复引用防护：TreeItemBase 未重写 Equals，HashSet 默认按引用比较。
            // WASM 下 StackOverflowException 是致命且不可恢复的（整个应用直接崩掉），
            // 因此任何节点在同一轮遍历中只允许被处理一次。
            var visited = new HashSet<TreeItemBase>();

            if (DataFormat == DataFormat.Children)
            {
                foreach (var node in DataSource.Where(x => x != null))
                {
                    PrepareChildrenNode(node, null, visited);
                }

                return;
            }

            var childrenByParent = DataSource
                .Where(x => x != null)
                .GroupBy(x => x.ParentId)
                .ToDictionary(x => x.Key, x => x.ToList());

            foreach (var node in childrenByParent.TryGetValue(0, out var roots) ? roots : DataSource.Where(x => x?.ParentId == 0))
            {
                PrepareListNode(node, null, childrenByParent, visited);
            }
        }

        private void PrepareListNode(TreeItemBase node, TreeItemBase parent, IDictionary<int, List<TreeItemBase>> childrenByParent, HashSet<TreeItemBase> visited)
        {
            if (node == null || !visited.Add(node))
            {
                // 数据源存在环或重复引用时直接跳过，避免无限递归（WASM 下会致命崩溃）
                return;
            }

            PrepareNode(node, parent);

            // Id == 0 是“根”的哨兵值（ParentId == 0 表示根节点），所以 Id 为 0 的节点
            // 自身就是根，不能再拿 childrenByParent[0] 当它的子节点 ——
            // 否则它会把自己（以及所有兄弟根）当成自己的孩子而无限自我递归。
            var children = node.Id != 0 && childrenByParent.TryGetValue(node.Id, out var found)
                ? found.Where(x => x != null && !ReferenceEquals(x, node)).ToList()
                : new List<TreeItemBase>();

            var hasChildren = children.Count > 0;
            node.HasChildren = node.HasChildren || hasChildren;
            if (AutoExpandAll && hasChildren)
            {
                node.Expanded = true;
            }

            foreach (var child in children)
            {
                PrepareListNode(child, node, childrenByParent, visited);
            }
        }

        private void PrepareChildrenNode(TreeItemBase node, TreeItemBase parent, HashSet<TreeItemBase> visited)
        {
            if (node == null || !visited.Add(node))
            {
                // 数据源自身成环（子节点反向指回祖先）时跳过，避免无限递归
                return;
            }

            PrepareNode(node, parent);
            var children = node.Children?.Where(x => x != null).ToList() ?? new List<TreeItemBase>();
            node.HasChildren = node.HasChildren || children.Any();
            if (AutoExpandAll && children.Any())
            {
                node.Expanded = true;
            }

            foreach (var child in children)
            {
                if (child.ParentId == 0)
                {
                    child.ParentId = node.Id;
                }
                child.Parent = node;
                PrepareChildrenNode(child, node, visited);
            }
        }

        private static void PrepareNode(TreeItemBase node, TreeItemBase parent)
        {
            node.Level = (parent?.Level ?? -1) + 1;
            node.TextPaths = parent?.TextPaths == null
                ? new Dictionary<int, string>()
                : new Dictionary<int, string>(parent.TextPaths);
            if (!node.TextPaths.ContainsKey(node.Id))
            {
                node.TextPaths.Add(node.Id, node.Text);
            }
        }

        private static IEnumerable<TreeItemBase> FlattenTreeItems(IEnumerable<TreeItemBase> nodes, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            foreach (var node in nodes ?? Enumerable.Empty<TreeItemBase>())
            {
                if (node == null || !visited.Add(node))
                {
                    continue;
                }

                yield return node;
                foreach (var child in FlattenTreeItems(node.Children, visited))
                {
                    yield return child;
                }
            }
        }
    }
}
