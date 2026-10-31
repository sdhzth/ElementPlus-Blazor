using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 高性能虚拟滚动树（官方 element-plus TreeV2 对齐）
    /// 核心：把树拍平成一维数组，只渲染可视区节点 + overscan
    /// </summary>
    public partial class ElTreeV2 : ElTree, IAsyncDisposable
    {
        private ElementReference _bodyRef;
        private IJSObjectReference _jsModule;
        private bool _moduleLoaded;
        private bool _disposed;

        #region 参数

        /// <summary>虚拟滚动必需，节点高度（默认 48px）</summary>
        [Parameter]
        public int ItemSize { get; set; } = 48;

        /// <summary>容器总高度（虚拟滚动必需）</summary>
        [Parameter]
        public int Height { get; set; } = 500;

        /// <summary>容器宽度</summary>
        [Parameter]
        public string Width { get; set; } = "100%";

        /// <summary>预渲染行数（减少滚动白屏）</summary>
        [Parameter]
        public int OverscanCount { get; set; } = 5;

        /// <summary>始终显示滚动条（官方 scrollbar-always-on，2.10.4）</summary>
        [Parameter]
        public bool ScrollbarAlwaysOn { get; set; } = false;

        /// <summary>过滤方法（官方 filter-method）：返回 true 表示节点可见</summary>
        [Parameter]
        public Func<TreeItemBase, string, bool> FilterMethod { get; set; }

        #endregion

        /// <summary>
        /// 构造函数：保留虚拟树「默认不展开全部」的意图。
        /// AutoExpandAll 由基类 <see cref="ElTree"/> 声明为参数（默认 true），
        /// ElTreeV2 在此把默认值覆盖为 false，避免实例化后字段初始化器先置 true。
        /// 若调用方显式传参，Blazor 会在 SetParametersAsync 中覆盖本默认值。
        /// </summary>
        public ElTreeV2()
        {
            AutoExpandAll = false;
        }

        #region 计算属性

        protected int ItemHeightSafe => Math.Max(20, ItemSize);
        protected int OverscanSafe => Math.Max(0, OverscanCount);

        /// <summary>
        /// 拍平后的一维节点数组（所有展开节点按深度顺序）
        /// </summary>
        protected IReadOnlyList<TreeItemBase> FlattenedNodes
        {
            get
            {
                var result = new List<TreeItemBase>();
                if (DataSource == null) return result;
                var roots = DataFormat == DataFormat.List
                    ? DataSource.Where(x => x.ParentId == 0).ToList()
                    : DataSource.ToList();

                foreach (var root in roots)
                {
                    Flatten(root, result);
                }
                return result;
            }
        }

        private void Flatten(TreeItemBase node, List<TreeItemBase> list, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                // 数据源成环或重复引用：直接跳过，避免无限递归
                // （WASM 下 StackOverflowException 是致命且不可恢复的，会拖垮整个应用）
                return;
            }

            if (!V2IsVisible(node)) return;
            list.Add(node);
            var children = GetV2Children(node);

            if (node.Expanded || AutoExpandAll)
            {
                foreach (var child in children)
                {
                    Flatten(child, list, visited);
                }
            }
        }

        /// <summary>
        /// 取得节点的子节点，并规避「根哨兵值」与「无限递归」两类陷阱。
        /// <para>
        /// 1. List 模式下 <c>ParentId == 0</c> 表示根节点，所以 <c>Id == 0</c> 的节点自身就是根，
        ///    绝不能再拿 <c>Id == 0</c> 去反查子节点 —— 否则会把自己和所有兄弟根都当成自己的孩子，
        ///    形成自我递归。这与 ElTreeSelect 历史上导致页面崩溃的原因完全一致。
        /// </para>
        /// <para>2. 统一过滤 null 子节点，避免下游 foreach 触发 NullReferenceException。</para>
        /// </summary>
        private List<TreeItemBase> GetV2Children(TreeItemBase node)
        {
            if (node == null)
            {
                return new List<TreeItemBase>();
            }

            if (DataFormat == DataFormat.List)
            {
                if (node.Id == 0)
                {
                    return new List<TreeItemBase>();
                }

                return DataSource?.Where(x => x != null && x.ParentId == node.Id).ToList()
                       ?? new List<TreeItemBase>();
            }

            return node.Children?.Where(x => x != null).ToList() ?? new List<TreeItemBase>();
        }

        private bool V2IsVisible(TreeItemBase item)
        {
            if (string.IsNullOrWhiteSpace(FilterText)) return true;
            if (FilterNodeMethod != null && FilterNodeMethod(item, FilterText)) return true;
            if (FilterMethod != null && FilterMethod(item, FilterText)) return true;
            if (FilterNodeMethod == null && FilterMethod == null
                && (item.Text ?? string.Empty).Contains(FilterText, StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            return GetAllChildren(item).Any(V2IsVisible);
        }

        private int _scrollTop;

        protected int StartIndex
        {
            get
            {
                var start = _scrollTop / ItemHeightSafe;
                return Math.Max(0, start - OverscanSafe);
            }
        }

        protected int EndIndex
        {
            get
            {
                var visible = Height / ItemHeightSafe;
                var end = (_scrollTop / ItemHeightSafe) + (int)Math.Ceiling((double)visible) + OverscanSafe;
                return Math.Min(FlattenedNodes.Count - 1, end);
            }
        }

        protected IReadOnlyList<TreeItemBase> VisibleNodes
        {
            get
            {
                if (FlattenedNodes.Count == 0) return new List<TreeItemBase>();
                var s = StartIndex;
                var e = EndIndex;
                if (s > e) return new List<TreeItemBase>();
                return FlattenedNodes.Skip(s).Take(e - s + 1).ToList();
            }
        }

        protected int TotalPaddingTop => StartIndex * ItemHeightSafe;
        protected int TotalPaddingBottom => Math.Max(0, (FlattenedNodes.Count - 1 - EndIndex) * ItemHeightSafe);

        protected string TreeV2Class => HtmlPropertyBuilder.CreateCssClassBuilder()
            .Add("el-tree el-tree-v2", Cls)
            .AddIf(Draggable, "is-draggable")
            .AddIf(HighlightCurrent, "el-tree--highlight-current")
            .AddIf(ScrollbarAlwaysOn, "is-scrollbar-always-on")
            .ToString();

        #endregion

        #region 生命周期

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // 初始化展开状态 + 计算 Level
            if (DataSource != null)
            {
                foreach (var root in GetRootNodes())
                {
                    InitLevel(root, 0);
                    if (AutoExpandAll && !root.Expanded)
                    {
                        root.Expanded = true;
                        SetChildrenExpanded(root, true);
                    }
                }

                if (DefaultExpandedKeys != null)
                {
                    foreach (var id in DefaultExpandedKeys)
                    {
                        var node = GetAllNodes().FirstOrDefault(n => n.Id == id);
                        if (node != null) node.Expanded = true;
                    }
                }

                if (DefaultCheckedKeys != null && ShowCheckbox)
                {
                    foreach (var id in DefaultCheckedKeys)
                    {
                        var node = GetAllNodes().FirstOrDefault(n => n.Id == id);
                        if (node != null)
                        {
                            node.Checked = true;
                            if (!CheckStrictly) SetChildrenChecked(node, true);
                        }
                    }
                }
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender && !_moduleLoaded)
            {
                try
                {
                    _jsModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                        "import", "./_content/Element-Plus/js/dom.js");
                    _moduleLoaded = true;
                }
                catch { _moduleLoaded = false; }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try { if (_jsModule != null) await _jsModule.DisposeAsync(); } catch { }
        }

        #endregion

        #region 辅助方法

        private List<TreeItemBase> GetRootNodes()
        {
            if (DataSource == null) return new List<TreeItemBase>();
            return DataFormat == DataFormat.List
                ? DataSource.Where(x => x.ParentId == 0).ToList()
                : DataSource.ToList();
        }

        private List<TreeItemBase> GetAllNodes()
        {
            if (DataSource == null) return new List<TreeItemBase>();
            return DataSource.ToList();
        }

        private void InitLevel(TreeItemBase node, int level, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                return;
            }

            node.Level = level;
            var children = GetV2Children(node);
            foreach (var child in children) InitLevel(child, level + 1, visited);
        }

        private void SetChildrenExpanded(TreeItemBase node, bool expanded)
        {
            var children = GetAllChildren(node);
            foreach (var c in children) c.Expanded = expanded;
        }

        private void SetChildrenChecked(TreeItemBase node, bool isChecked, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                return;
            }

            // 直接沿子节点递归即可。原实现先取「全部子孙」再逐个递归，
            // 在大树上会产生 O(n²) 的重复遍历，且没有环保护。
            foreach (var child in GetV2Children(node))
            {
                child.Checked = isChecked;
                SetChildrenChecked(child, isChecked, visited);
            }
        }

        private List<TreeItemBase> GetAllChildren(TreeItemBase node, HashSet<TreeItemBase> visited = null)
        {
            visited ??= new HashSet<TreeItemBase>();
            var result = new List<TreeItemBase>();
            if (node == null || !visited.Add(node))
            {
                return result;
            }

            var children = GetV2Children(node);
            result.AddRange(children);
            foreach (var c in children)
            {
                result.AddRange(GetAllChildren(c, visited));
            }
            return result;
        }

        #endregion

        #region 官方 Exposes 方法

        /// <summary>展开指定 key 集合的节点（官方 setExpandedKeys）</summary>
        public void SetExpandedKeys(IEnumerable<int> keys)
        {
            if (DataSource == null) return;
            var set = new HashSet<int>(keys ?? Enumerable.Empty<int>());
            foreach (var node in GetAllNodes())
            {
                if (set.Contains(node.Id)) node.Expanded = true;
            }
            if (AutoExpandParent)
            {
                foreach (var key in set)
                {
                    var node = GetAllNodes().FirstOrDefault(n => n.Id == key);
                    ExpandAncestors(node);
                }
            }
            Refresh();
        }

        private void ExpandAncestors(TreeItemBase node)
        {
            // 环保护：数据源里若存在 A.Parent = B、B.Parent = A 这类父指针成环，
            // 未加保护的向上遍历会无限循环（挂死 / OOM）。
            var visited = new HashSet<TreeItemBase>();
            var current = node?.Parent;
            while (current != null && visited.Add(current))
            {
                current.Expanded = true;
                current = current.Parent;
            }
        }

        /// <summary>展开指定节点（官方 expandNode）</summary>
        public void ExpandNode(int key)
        {
            var node = GetAllNodes().FirstOrDefault(n => n.Id == key);
            if (node != null) { node.Expanded = true; Refresh(); }
        }

        /// <summary>收起指定节点（官方 collapseNode）</summary>
        public void CollapseNode(int key)
        {
            var node = GetAllNodes().FirstOrDefault(n => n.Id == key);
            if (node != null) { node.Expanded = false; Refresh(); }
        }

        /// <summary>替换整棵数据（官方 setData）</summary>
        public void SetData(List<TreeItemBase> data)
        {
            DataSource = data;
            OnParametersSet();
            Refresh();
        }

        /// <summary>滚动到指定 key 的节点（官方 scrollTo）</summary>
        public async Task ScrollTo(int key)
        {
            var nodes = FlattenedNodes.ToList();
            var idx = nodes.FindIndex(n => n.Id == key);
            if (idx < 0) return;
            _scrollTop = idx * ItemHeightSafe;
            if (_moduleLoaded && _bodyRef.Id != null)
            {
                try { await JSRuntime.InvokeAsync<object>("elementSetElementScrollTop", _bodyRef, _scrollTop); } catch { }
            }
            StateHasChanged();
        }

        /// <summary>滚动到指定节点（官方 scrollToNode）</summary>
        public Task ScrollToNode(TreeItemBase node)
        {
            return node == null ? Task.CompletedTask : ScrollTo(node.Id);
        }

        #endregion

        #region 交互事件

        protected async Task OnBodyScrollAsync()
        {
            try
            {
                var top = await JSRuntime.InvokeAsync<int>("elementGetScrollTop", _bodyRef);
                if (top == _scrollTop) return;
                _scrollTop = top;
                StateHasChanged();
            }
            catch { }
        }

        protected async Task ToggleNodeAsync(TreeItemBase node)
        {
            if (node.Disabled) return;
            var children = GetAllChildren(node);
            if (children.Count == 0 && !node.HasChildren) return;

            node.Expanded = !node.Expanded;
            Refresh();

            if (node.Expanded)
            {
                if (OnNodeExpand.HasDelegate) await OnNodeExpand.InvokeAsync(node);
            }
            else
            {
                if (OnNodeCollapse.HasDelegate) await OnNodeCollapse.InvokeAsync(node);
            }
        }

        protected async Task OnNodeClickAsync(TreeItemBase node)
        {
            if (node.Disabled) return;
            var children = GetAllChildren(node);
            var hasChildren = node.HasChildren || children.Count > 0;
            if (ExpandOnClickNode) await ToggleNodeAsync(node);
            if (ShowCheckbox && ((!hasChildren && CheckOnClickLeaf) || (hasChildren && CheckOnClickNode)))
            {
                await OnNodeCheckAsync(node);
            }
            if (OnNodeClick.HasDelegate) await OnNodeClick.InvokeAsync(node);
        }

        protected async Task OnNodeDblClickAsync(TreeItemBase node)
        {
            if (node.Disabled) return;
            if (OnNodeDblClick.HasDelegate) await OnNodeDblClick.InvokeAsync(node);
        }

        protected async Task OnNodeCheckAsync(TreeItemBase node)
        {
            node.Checked = !node.Checked;
            if (!CheckStrictly) SetChildrenChecked(node, node.Checked);
            if (OnCheck.HasDelegate) await OnCheck.InvokeAsync(node);
            if (OnCheckChange.HasDelegate) await OnCheckChange.InvokeAsync(GetCheckedNodes());
            Refresh();
        }

        /// <summary>
        /// 刷新视图。基类 <see cref="ElTree.Refresh"/> 已是 virtual 且实现同样只调用 StateHasChanged，
        /// 这里必须显式 override：若写成隐藏（new / 无关键字），通过基类引用调用时会落到基类实现，
        /// 导致 V2 的刷新被跳过（编译器警告 CS0114）。
        /// </summary>
        public override void Refresh()
        {
            StateHasChanged();
        }

        #endregion

        #region Render Tree

        protected RenderFragment RenderNode(TreeItemBase node)
        {
            return builder => BuildNode(builder, node);
        }

        private void BuildNode(RenderTreeBuilder builder, TreeItemBase node)
        {
            var seq = 0;
            var children = GetAllChildren(node);
            var hasChildren = node.HasChildren || children.Count > 0;

            builder.OpenElement(seq++, "div");
            builder.SetKey(node.Id);
            builder.AddAttribute(seq++, "role", "treeitem");
            builder.AddAttribute(seq++, "tabindex", node.Disabled ? "-1" : "0");
            builder.AddAttribute(seq++, "aria-disabled", node.Disabled.ToString().ToLowerInvariant());
            builder.AddAttribute(seq++, "class", BuildNodeClass(node, hasChildren));
            builder.AddAttribute(seq++, "oncontextmenu", EventCallback.Factory.Create<MouseEventArgs>(this, _ =>
            {
                if (OnNodeContextMenu.HasDelegate) OnNodeContextMenu.InvokeAsync(node);
            }));

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class", "el-tree-node__content");
            builder.AddAttribute(seq++, "style", $"padding-left:{(Indent * (node.Level ?? 0))}px;height:{ItemHeightSafe}px;");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => OnNodeClickAsync(node)));
            builder.AddAttribute(seq++, "ondblclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => OnNodeDblClickAsync(node)));

            // 展开箭头
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", BuildIconClass(node, hasChildren));
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, _ => ToggleNodeAsync(node)));
            builder.AddEventStopPropagationAttribute(seq++, "onclick", true);
            builder.CloseElement();

            // Checkbox
            if (ShowCheckbox)
            {
                builder.OpenComponent<ElCheckbox<bool>>(seq++);
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.Status), node.Checked ? Status.Checked : Status.UnChecked);
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.IsDisabled), node.Disabled);
                builder.AddAttribute(seq++, nameof(ElCheckbox<bool>.StatusChanged), EventCallback.Factory.Create<Status>(this, _ => OnNodeCheckAsync(node)));
                builder.CloseComponent();
            }

            // 节点文字 / NodeTemplate
            builder.OpenElement(seq++, "span");
            builder.AddAttribute(seq++, "class", "el-tree-node__label");
            if (NodeTemplate != null)
                builder.AddContent(seq++, NodeTemplate(node));
            else
                builder.AddContent(seq++, node.Text);
            builder.CloseElement();

            builder.CloseElement(); // node__content
            builder.CloseElement(); // node
        }

        private string BuildNodeClass(TreeItemBase node, bool hasChildren)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-tree-node", "is-focusable")
                .AddIf(node.Expanded, "is-expanded")
                .AddIf(node.Disabled, "is-disabled")
                .ToString();
        }

        private string BuildIconClass(TreeItemBase node, bool hasChildren)
        {
            return HtmlPropertyBuilder.CreateCssClassBuilder()
                .Add("el-tree-node__expand-icon",
                    string.IsNullOrWhiteSpace(NodeIcon) ? "el-icon-caret-right" : NodeIcon,
                    node.Expanded ? "expanded" : string.Empty)
                .AddIf(!hasChildren, "is-leaf")
                .ToString();
        }

        #endregion
    }
}