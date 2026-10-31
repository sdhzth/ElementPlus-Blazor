using ElementPlus.ControlRender;
using ElementPlus.ControlRenders;
using ElementPlus.Core;
using ElementPlus.DisplayRenders;
using ElementPlus.Lang;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ElementPlus
{
    public static class Extension
    {
        /// <summary>
        /// 注册 Element 相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddElementServices(this IServiceCollection services)
        {
            services.AddSingleton<FormFieldControlMap>();
            services.AddSingleton<TableEditorMap>();
            services.AddScoped<Document>();
            services.AddScoped<MessageService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<ElNotification>();
            services.AddScoped<LoadingService>();
            // LoadingService 原来是 Singleton：整屏/区域 Loading 的队列是服务上的可变集合，
            // 而 ElPopup.OnInitialized 会把它重建一次（接管所有权）。Singleton 让这份队列跨连接共享，
            // Blazor Server 上后来者会把前一个 circuit 正在显示的遮罩接管走（前一个宿主再也收不到通知，
            // 遮罩关不掉、后续遮罩不出现）。改成 Scoped 与其余浮层服务同一口径：
            // MessageService / NotificationService / DialogService / PopupService 全是 AddScoped。

            services.AddScoped<DialogService>();
            services.AddScoped<ElDialogGrowService>();
            services.AddScoped<PopupService>();
            services.AddScoped<FullScreenService>();
            services.AddScoped<MessageBox>();
            services.AddScoped<IInputRender, InputRender>();
            services.AddScoped<ISelectRender, SelectRender>();
            services.AddScoped<ISwitchRender, SwitchRender>();
            services.AddScoped<IDatePickerRender, EmptyRender>();
            services.AddScoped<IDateTimePickerRender, DateTimePickerRender>();
            services.AddScoped<IDropDownTableRender, DropDownTableRender>();
            services.AddScoped<IUploadRender, UploadRender>();
            services.AddScoped<ITableRender, TableRender>();
            services.AddSingleton<DisplayRenderFactory>();
            services.AddSingleton<DateTimeRender>();
            services.AddSingleton<EnumRender>();
            services.AddSingleton<GenericRender>();
            // 库级词条（i18n）：注册成 Scoped 而不是 Singleton —— 宿主普遍把 HttpClient 注册为 Scoped，
            // Singleton 去消费它会直接抛 InvalidOperationException；工厂里 GetService 取不到 HttpClient 时
            // ElementLang 自动退化为「一切取词走 fallback」，组件照常渲染。
            services.AddScoped<ILang>(sp => new ElementLang(sp.GetService<HttpClient>()));
            return services;
        }

    }
}
