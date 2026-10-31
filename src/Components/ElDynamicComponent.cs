using ElementPlus.ControlConfigs;
using ElementPlus.ControlRender;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ElementPlus
{
    /// <summary>
    /// 提供动态组件渲染的功能
    /// </summary>
    public class ElDynamicComponent : ComponentBase
    {
        [Inject]
        private IServiceProvider provider { get; set; }
        [Parameter]
        public object Component { get; set; }

        /// <summary>
        /// 已经加载过数据源的 Config 实例（引用比较），避免父级每次重渲染都重复拉取数据。
        /// </summary>
        private RenderConfig loadedConfig;
        private Type loadedLoaderType;

        protected override async Task OnParametersSetAsync()
        {
            if (Config?.DataSourceLoader == null)
            {
                return;
            }

            // 父级重渲染会再次触发 OnParametersSetAsync；若不缓存，
            // 每一次父级重渲染都会重新 LoadAsync() 并 StateHasChanged()，
            // 造成重复的数据加载风暴。仅当 Config 实例或加载器类型变化时才重新加载。
            if (ReferenceEquals(loadedConfig, Config) && loadedLoaderType == Config.DataSourceLoader)
            {
                return;
            }

            loadedConfig = Config;
            loadedLoaderType = Config.DataSourceLoader;

            var loader = (IDataSourceLoader)provider.GetRequiredService(Config.DataSourceLoader);
            Config.DataSource = await loader.LoadAsync();
            StateHasChanged();
        }
        /// <summary>
        /// 渲染配置
        /// </summary>
        [Parameter]
        public RenderConfig Config { get; set; }

        [Parameter]
        public IDictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var render = Component as IControlRender;
            if (render != null)
            {
                if (Config == null)
                {
                    throw new ElementException("Component 不能为 IControlRender 时必须指定 Config 参数");
                }

                // 让本组件成为编辑器双向绑定的回调接收者：Blazor 在事件回调结束后标记
                // <b>接收者</b>重渲染，因此一次按键只会重渲染这个单元格子树，
                // 而不是整张表（宿主 ElTable 里可能几十行 × 几十列）。
                // 宿主若需要"改一个格子、整表跟着变"，走 RenderConfig.ValueChanged 通知，别依赖这里。
                Config.Owner = this;

                render.Render(builder, Config);
                return;
            }

            else if (Component is Type type)
            {
                builder.OpenComponent(0, type);
                var seq = 1;
                foreach (var key in Parameters.Keys)
                {
                    builder.AddAttribute(seq++, key, Parameters[key]);
                }
                builder.CloseComponent();
                return;
            }
            if (Component is RenderFragment renderFragment)
            {
                builder.AddContent(0, renderFragment);
                return;
            }
            builder.AddMarkupContent(0, Convert.ToString(Component));
        }
    }
}
