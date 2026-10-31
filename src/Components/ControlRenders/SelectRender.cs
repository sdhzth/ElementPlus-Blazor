using ElementPlus.ControlConfigs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ElementPlus.ControlRenders
{
    internal class SelectRender : RenderBase, ISelectRender
    {
        protected override void CreateBind(RenderConfig config, RenderTreeBuilder builder, int startIndex)
        {
            base.CreateBind(config, builder, startIndex);
            builder.AddAttribute(startIndex + 3, nameof(ElSelect<string>.LabelChanged), (Action<string>)(label => config.RawLabel = label));
            builder.AddAttribute(startIndex + 4, nameof(ElSelect<string>.Label), config.RawLabel?.ToString());
        }
        public void Render(RenderTreeBuilder renderTreeBuilder, RenderConfig renderConfig)
        {
            var valueType = renderConfig.Property.PropertyType;
            var nullValueType = Nullable.GetUnderlyingType(valueType);
            var finalValueType = nullValueType ?? valueType;
            var hasExplicitDataSource = renderConfig.DataSource != null
                && !string.IsNullOrWhiteSpace(renderConfig.ValueField);
            if (renderConfig.InputControlType.IsGenericTypeDefinition && !finalValueType.IsEnum && !hasExplicitDataSource)
            {
                throw new ElementException("下拉框生成只支持List或枚举类型");
            }
            else if (renderConfig.InputControlType.IsGenericTypeDefinition)
            {
                renderConfig.InputControlType = renderConfig.InputControlType.MakeGenericType(valueType);
            }
            if (hasExplicitDataSource)
            {
                RenderFromDataSource(renderTreeBuilder, renderConfig);
                return;
            }
            renderTreeBuilder.OpenComponent(0, renderConfig.InputControlType);
            try
            {
                var selectAttr = renderConfig.Property.GetCustomAttribute<SelectAttribute>();
                if (selectAttr != null)
                {
                    var dataSource = renderConfig.DataSource;
                    var dataSourceType = GetDataSourceItemType(dataSource);
                    var valueProperty = dataSourceType.GetProperty(selectAttr.Value);
                    var textProperty = dataSourceType.GetProperty(selectAttr.Display);
                    var seq = 17;
                    FormControlRender.AddSelectAttributes(renderTreeBuilder, selectAttr, ref seq);
                    renderTreeBuilder.AddAttribute(1, nameof(ElSelect<string>.ChildContent), (RenderFragment)(builder =>
                    {
                        foreach (var item in dataSource as IEnumerable)
                        {
                            builder.OpenComponent(2, typeof(ElOption<>).MakeGenericType(valueProperty.PropertyType));
                            builder.AddAttribute(3, nameof(ElOption<string>.Value), valueProperty.GetValue(item));
                            builder.AddAttribute(4, nameof(ElOption<string>.Text), textProperty.GetValue(item));
                            builder.CloseComponent();
                        }
                    }));

                    CreateBind(renderConfig, renderTreeBuilder, 6);
                    renderTreeBuilder.AddComponentReferenceCapture(9, e => renderConfig.InputControl = e);
                    return;
                }
                else
                {
                    var selectAttribute = (SelectAttribute)renderConfig.ControlAttribute;
                    var seq = 17;
                    FormControlRender.AddSelectAttributes(renderTreeBuilder, selectAttribute, ref seq);
                }
                if (!finalValueType.IsEnum)
                {
                    throw new ElementException("下拉框生成只支持List或枚举类型");
                }

                renderTreeBuilder.AddAttribute(6, nameof(ElFormItemObject.ChildContent), (RenderFragment)(contentBuilder =>
                {
                    var names = Enum.GetNames(finalValueType);
                    for (int i = 0; i < names.Length; i++)
                    {
                        var name = names[i];
                        contentBuilder.OpenComponent(7, typeof(ElOption<>).MakeGenericType(renderConfig.Property.PropertyType));
                        var field = finalValueType.GetField(name);
                        var descAttr = field.GetCustomAttribute<DescriptionAttribute>();
                        var text = string.Empty;
                        if (descAttr != null)
                        {
                            text = descAttr.Description;
                        }
                        else
                        {
                            var displayAttr = field.GetCustomAttribute<DisplayAttribute>();
                            text = displayAttr?.Name ?? displayAttr?.Description;
                        }
                        contentBuilder.AddAttribute(8, nameof(ElOption<int>.Text), text ?? name);
                        contentBuilder.AddAttribute(9, nameof(ElOption<int>.Value), Enum.Parse(finalValueType, name));
                        contentBuilder.CloseComponent();
                    }
                }));
                renderTreeBuilder.AddAttribute(10, nameof(ElFormItemObject.EnableAlwaysRender), true);
                if (!string.IsNullOrWhiteSpace(renderConfig.Placeholder))
                {
                    renderTreeBuilder.AddAttribute(11, nameof(ElFormItemObject.Placeholder), renderConfig.Placeholder);
                }
                CreateBind(renderConfig, renderTreeBuilder, 13);
                renderTreeBuilder.AddComponentReferenceCapture(16, e => renderConfig.InputControl = e);

            }
            finally
            {
                renderTreeBuilder.CloseComponent();
            }


        }

        /// <summary>
        /// 列上显式给出 EditorDataSource + EditorValueField 时的下拉渲染：不依赖 SelectAttribute，
        /// 直接按 ValueField / TextField 反射生成 ElOption，且 ElOption 的泛型参数取自 ElSelect 的 TValue，
        /// 值再转成该类型，保证 ElOption&lt;T&gt; 与 ElSelect&lt;T&gt; 严格匹配。
        /// </summary>
        private void RenderFromDataSource(RenderTreeBuilder renderTreeBuilder, RenderConfig renderConfig)
        {
            var dataSource = renderConfig.DataSource as IEnumerable;
            if (dataSource == null)
            {
                throw new ElementException("EditorDataSource 必须是可枚举的集合");
            }
            var itemType = GetDataSourceItemType(dataSource);
            var valueProperty = itemType.GetProperty(renderConfig.ValueField);
            if (valueProperty == null)
            {
                throw new ElementException($"数据源项中不存在值字段 {renderConfig.ValueField}");
            }
            var textProperty = string.IsNullOrWhiteSpace(renderConfig.TextField)
                ? valueProperty
                : (itemType.GetProperty(renderConfig.TextField) ?? valueProperty);

            var tValue = renderConfig.InputControlType.GetGenericArguments()[0];
            var optionType = typeof(ElOption<>).MakeGenericType(tValue);
            var underlyingType = Nullable.GetUnderlyingType(tValue) ?? tValue;

            renderTreeBuilder.OpenComponent(0, renderConfig.InputControlType);
            renderTreeBuilder.AddAttribute(1, nameof(ElSelect<string>.ChildContent), (RenderFragment)(builder =>
            {
                var seq = 2;
                foreach (var item in dataSource)
                {
                    var rawValue = valueProperty.GetValue(item);
                    object optionValue = null;
                    if (rawValue != null)
                    {
                        optionValue = underlyingType.IsInstanceOfType(rawValue)
                            ? rawValue
                            : Convert.ChangeType(rawValue, underlyingType);
                    }
                    builder.OpenComponent(seq++, optionType);
                    builder.AddAttribute(seq++, nameof(ElOption<string>.Value), optionValue);
                    builder.AddAttribute(seq++, nameof(ElOption<string>.Text), Convert.ToString(textProperty.GetValue(item)));
                    builder.CloseComponent();
                }
            }));

            // 走基类的双向绑定 + SelectRender 的 Label / LabelChanged：
            // Label 给初始值（进入编辑时立刻显示当前值，不用等 ElSelect 自己反查回填），
            // LabelChanged 把选中后的新文本回写 RawLabel，宿主重渲染时 Label 才会跟着变。
            CreateBind(renderConfig, renderTreeBuilder, 6);
            // 序号必须避开 6/7（ValueChanged/Value）与 9/10（LabelChanged/Label），
            // 否则组件引用捕获会把前面的属性帧覆盖掉，绑定直接失效。
            renderTreeBuilder.AddComponentReferenceCapture(20, e => renderConfig.InputControl = e);
            renderTreeBuilder.CloseComponent();
        }

        private static Type GetDataSourceItemType(object dataSource)
        {
            if (dataSource == null)
            {
                return typeof(object);
            }
            var enumerableType = dataSource.GetType()
                .GetInterfaces()
                .Concat(new[] { dataSource.GetType() })
                .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            return enumerableType?.GetGenericArguments()[0] ?? typeof(object);
        }
    }
}
