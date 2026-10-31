using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ElementPlus.DisplayRenders
{
    internal class DisplayRenderFactory
    {
        private static IDictionary<Func<TableHeader, bool>, Type> renderMap = new Dictionary<Func<TableHeader, bool>, Type>();
        private readonly IServiceProvider provider;

        static DisplayRenderFactory()
        {
            renderMap.Add(x => x.Property.PropertyType == typeof(string), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(int), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(DateTime), typeof(DateTimeRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(int?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(DateTime?), typeof(DateTimeRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(bool), typeof(DateTimeRender));
            // 数值族此前缺失 → CreateRenderFactory 返回 null → Eval 为 null → 单元格显示空白且不可编辑。
            // GenericRender 只做 value?.ToString()，与具体类型无关，可直接承载这些数值类型。
            renderMap.Add(x => x.Property.PropertyType == typeof(decimal), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(decimal?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(double), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(double?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(float), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(float?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(long), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(long?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(short), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType == typeof(short?), typeof(GenericRender));
            renderMap.Add(x => x.Property.PropertyType.IsEnum, typeof(EnumRender));
            renderMap.Add(x => x.Property.PropertyType.IsGenericType && (Nullable.GetUnderlyingType(x.Property.PropertyType) ?? x.Property.PropertyType).IsEnum, typeof(EnumRender));
        }
        public DisplayRenderFactory(IServiceProvider provider)
        {
            this.provider = provider;
        }
        public IDisplayRender CreateRenderFactory(TableHeader tableHeader)
        {
            var type = renderMap.FirstOrDefault(x => x.Key(tableHeader)).Value;
            if (type == null)
            {
                return null;
            }
            return (IDisplayRender)provider.GetRequiredService(type);
        }
    }
}
