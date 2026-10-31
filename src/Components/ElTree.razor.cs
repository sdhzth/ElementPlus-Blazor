using ElementPlus.Model;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ElementPlus
{
    public partial class ElTree : ElementComponentBase
    {
        private List<TreeItemBase> selectedNodes = new List<TreeItemBase>();
        private List<TreeItemBase> items;

        /// <summary>
        /// 记录已经按 AutoExpandAll 展开过的数据源实例，保证「只自动展开一次」。
        /// </summary>
        private List<TreeItemBase> autoExpandedSource;
        [Inject]
        private HttpClient httpClient { get; set; }

        /// <summary>
        /// 一次性加载所有节点
        /// </summary>
        [Parameter]
        public bool? LoadFullTree { get; set; }
        /// <summary>
        /// 选择节点后触发
        /// </summary>
        [Parameter]
        public EventCallback<List<TreeItemBase>> SelectedNodesChanged { get; set; }

        /// <summary>
        /// 服务器地址
        /// </summary>
        [Parameter]
        public string Url { get; set; }

        /// <summary>
        /// 节点类型，必须实现 <seealso cref="TreeItemBase"/> 接口
        /// </summary>
        [Parameter]
        public Type ItemType { get; set; } = typeof(TreeItemBase);

        [CascadingParameter]
        public ElTreeSelect Select { get; set; }

        /// <summary>
        /// 数据格式
        /// </summary>
        [Parameter]
        public DataFormat DataFormat { get; set; } = DataFormat.List;

        /// <summary>
        /// 自动展开所有层级
        /// </summary>
        [Parameter]
        public bool AutoExpandAll { get; set; } = true;

        /// <summary>
        /// 数据源
        /// </summary>
        [Parameter]
        public List<TreeItemBase> DataSource { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            // 先把参数数据落到 items，后面所有遍历都基于它。
            // 原实现把 DefaultExpandedKeys 的处理放在赋值之前，纯 DataSource 绑定
            // （未配置 Url）时 items 仍为 null，会抛 NullReferenceException。
            if (DataSource != null)
            {
                items = DataSource;
            }

            // items 为 null 说明数据还没准备好（例如只配置了 Url），直接返回。
            if (items == null)
            {
                return;
            }

            // AutoExpandAll（默认 true）对「直接绑定 DataSource」也应当生效。
            // 原实现只在 ExpandAsync / AddChild 中调用 AutoExpandAllAsync，
            // 纯数据绑定不会展开任何节点。这里按数据源实例只展开一次，
            // 避免每次参数更新都把用户手动折叠的节点重新展开。
            if (AutoExpandAll && !ReferenceEquals(autoExpandedSource, items))
            {
                autoExpandedSource = items;
                foreach (var node in items)
                {
                    node.Expanded = true;
                }
            }

            // 官方 DefaultExpandedKeys / DefaultCheckedKeys 支持
            if (DefaultExpandedKeys != null && DefaultExpandedKeys.Count > 0)
            {
                foreach (var node in items)
                {
                    if (DefaultExpandedKeys.Contains(node.Id))
                    {
                        node.Expanded = true;
                    }
                }
            }

            if (DefaultCheckedKeys != null && DefaultCheckedKeys.Count > 0)
            {
                foreach (var node in items)
                {
                    if (DefaultCheckedKeys.Contains(node.Id))
                    {
                        node.Checked = true;
                    }
                }
            }

            // 官方 current-node-key：按 key 高亮当前节点
            if (CurrentNodeKey != null)
            {
                var cur = items.FirstOrDefault(x => x.Id == CurrentNodeKey.Value);
                if (cur != null)
                {
                    if (!selectedNodes.Contains(cur))
                    {
                        selectedNodes.Add(cur);
                    }
                    _currentNode = cur;
                }
            }

            // 行的缩进来自 node.Level（渲染时 padding-left = Indent × Level）。
            // 以前只有懒加载那条路径会算 Level，纯 DataSource 绑定下它始终是 null，
            // 结果整个树每一行的 padding-left 都是 0px —— 即使层级对了也看不出父子关系。
            InitFeatureLevels();
        }

        protected override async Task OnParametersSetAsync()
        {
            if (!RequireRender)
            {
                return;
            }
            await FetchNodesAsync();
            await InvokeAsync(Refresh);
        }

        private async Task FetchNodesAsync()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                return;
            }
            EnsureUrl();
            var responseObj = (IEnumerable)await httpClient.GetFromJsonAsync(Url, typeof(List<>).MakeGenericType(ItemType));
            DataSource = items = responseObj.Cast<TreeItemBase>().ToList();
            foreach (var item in DataSource)
            {
            }
            var rootNodes = FindChildren(new TreeItemModel()
            {
                Children = items
            });
            Initilize(rootNodes, null);
        }

        private void EnsureUrl()
        {
            if (!Url.EndsWith("/"))
            {
                Url += "/";
            }
        }

        private async Task SelectNodeAsync(TreeItemBase treeNode)
        {
            selectedNodes.Add(treeNode);
            if (SelectedNodesChanged.HasDelegate)
            {
                _ = SelectedNodesChanged.InvokeAsync(selectedNodes);
            }
            if (Select != null)
            {
                await Select.OnInternalSelectAsync(new SelectResultModel<int?>()
                {
                    Text = treeNode.TextPath,
                    Key = treeNode.Id
                });
            }
        }

        private void Initilize(List<TreeItemBase> nodes, TreeItemBase parentNode)
        {
            Initilize(nodes, parentNode, new HashSet<TreeItemBase>());
        }

        private void Initilize(List<TreeItemBase> nodes, TreeItemBase parentNode, HashSet<TreeItemBase> visited)
        {
            foreach (var node in nodes)
            {
                // Defensive guard: skip nulls and already visited nodes so that
                // duplicated ids / cyclic parent-child data cannot recurse forever.
                if (node == null || !visited.Add(node))
                {
                    continue;
                }

                node.Level = (parentNode?.Level ?? -1) + 1;
                node.TextPaths = parentNode?.TextPaths == null ? new Dictionary<int, string>() : new Dictionary<int, string>(parentNode.TextPaths);
                if (!node.TextPaths.ContainsKey(node.Id))
                {
                    node.TextPaths.Add(node.Id, node.Text);
                }
                Initilize(FindChildren(node), node, visited);
            }
        }

        /// <summary>
        /// 展开某个节点
        /// </summary>
        /// <param name="treeItem"></param>
        public async Task ExpandAsync(TreeItemBase treeItem, bool autoRefresh = true)
        {
            if (treeItem.Expanded)
            {
                return;
            }
            treeItem.Expanded = true;
            if (LoadFullTree != null && LoadFullTree.Value)
            {
                var children = FindChildren(treeItem);
                await ExpandAsync(children, false);
                if (autoRefresh)
                {
                    Refresh();
                }
                return;
            }
            if (!string.IsNullOrWhiteSpace(Url))
            {
                EnsureUrl();
                var responseObj = (IEnumerable)await httpClient.GetFromJsonAsync(Url + treeItem.Id, typeof(List<>).MakeGenericType(ItemType));
                var newTreeNodes = responseObj.Cast<TreeItemBase>().ToArray();
                DataSource.RemoveAll(x => newTreeNodes.Any(y => y.Id == x.Id));
                DataSource.AddRange(newTreeNodes);
                var rootNodes = FindChildren(new TreeItemModel()
                {
                    Children = items
                });
                Initilize(rootNodes, null);
                await AutoExpandAllAsync();
            }
            Refresh();
        }

        private async Task AutoExpandAllAsync()
        {
            if (DataSource == null)
            {
                return;
            }
            if (!AutoExpandAll)
            {
                return;
            }
            var dataSource = DataSource.ToList();
            if (LoadFullTree == null && dataSource.Any(x => x.Children != null && x.Children.Any()))
            {
                LoadFullTree = true;
            }
            await ExpandAsync(dataSource);
        }

        /// <summary>
        /// 按「数据源实例」只自动展开一次。
        /// <para>
        /// 这里必须做去重：<see cref="OnAfterRenderAsync"/> 在**每一次**渲染后都会跑，
        /// 若在里面无条件展开全部节点，用户刚点箭头折叠掉的节点会在同一次交互的收尾
        /// 被立刻重新展开 —— 表现就是「点展开/收缩完全没有反应」。
        /// 真正需要「每次渲染都展开」的场景并不存在：数据源没换，就不该动展开状态。
        /// </para>
        /// <para>
        /// Url 远程取数这条路径经由 <see cref="FetchNodesAsync"/> 换掉 <c>items</c>、
        /// 且不会再走 <see cref="OnParametersSet"/>，所以仍然靠这里完成首次展开。
        /// </para>
        /// </summary>
        private async Task AutoExpandAllOnceAsync()
        {
            if (items == null || DataSource == null || !AutoExpandAll)
            {
                return;
            }

            if (ReferenceEquals(autoExpandedSource, items))
            {
                return;
            }

            autoExpandedSource = items;
            await AutoExpandAllAsync();
        }

        private async Task ExpandAsync(List<TreeItemBase> dataSource, bool autoRefresh = true)
        {
            foreach (var item in dataSource)
            {
                await ExpandAsync(item, autoRefresh);
            }
        }

        private async Task ToggleAsync(TreeItemBase item)
        {
            if (item.Expanded)
            {
                item.Expanded = false;
                Refresh();
            }
            else
            {
                await ExpandAsync(item);
            }
            item.Expanded = !item.Expanded;
        }

        private List<TreeItemBase> FindChildren(TreeItemBase treeItem)
        {
            switch (DataFormat)
            {
                case DataFormat.List:
                    return items.Where(x => x.ParentId == treeItem.Id).ToList();
                case DataFormat.Children:
                    return treeItem.Children ?? new List<TreeItemBase>();
            }
            return new List<TreeItemBase>();
        }

        internal void AddChild(TreeItemModel treeItemModel)
        {
            items = items ?? new List<TreeItemBase>();
            var parent = items.FirstOrDefault(x => x.Id == treeItemModel.ParentId);
            treeItemModel.Level = (parent?.Level ?? -1) + 1;
            treeItemModel.TextPaths = parent?.TextPaths == null
                ? new Dictionary<int, string>()
                : new Dictionary<int, string>(parent.TextPaths);
            if (!treeItemModel.TextPaths.ContainsKey(treeItemModel.Id))
            {
                treeItemModel.TextPaths.Add(treeItemModel.Id, treeItemModel.Text);
            }
            treeItemModel.Expanded = AutoExpandAll;
            items.Add(treeItemModel);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            if (firstRender)
            {
                Refresh();
                return;
            }

            // 注意：这里**不能**无条件 ExpandAsync 全部节点。
            // 本方法在每次渲染后都会执行，无条件展开会把用户刚折叠的节点顶回去，
            // 导致「点箭头毫无反应」（详见 AutoExpandAllOnceAsync 的注释）。
            await AutoExpandAllOnceAsync();
        }
    }
}