using Bunit;
using ElementPlus;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using Xunit;

namespace ElementPlus.ComponentTests
{
    public class ElMentionTests : BunitContext
    {
        public ElMentionTests()
        {
            Services.AddElementPlusServices();
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 2, 2 });
            JSInterop.SetupVoid("elementMentionSetSelection", _ => true).SetVoidResult();
            JSInterop.SetupVoid("execFocus", _ => true).SetVoidResult();
        }

        [Fact]
        public void FiltersAndSelectsMentionWithKeyboard()
        {
            string value = null;
            MentionOption selected = null;
            string changed = null;
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" },
                    new MentionOption { Label = "Bob", Value = "bob" }
                })
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.OnSelect, next => selected = next)
                .Add(x => x.OnChange, next => changed = next)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@b");
            Assert.Single(cut.FindAll(".el-select-dropdown__item"));

            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal("@bob", value);
            Assert.Equal("bob", selected.Value);
            Assert.Equal("@bob", changed);
        }

        [Fact]
        public void SupportsMultipleTriggerCharacters()
        {
            string value = null;
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 4, 4 });

            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Prefix, "@")
                .Add(x => x.Prefixes, new[] { "@", "#" })
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Topic", Value = "topic" }
                })
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("#to");
            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal("#topic", value);
        }

        [Fact]
        public void ReplacesMentionAtCaretInsideTextareaContent()
        {
            string value = "Hello @al world";
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 9, 9 });

            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("Hello @al world");
            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal("Hello @alice world", value);
        }

        [Fact]
        public void KeyboardNavigationSkipsDisabledItemsAndTabSelects()
        {
            string value = null;
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 2, 2 });

            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice", Disabled = true },
                    new MentionOption { Label = "Bob", Value = "bob" }
                })
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@b");
            var option = cut.Find(".el-select-dropdown__item");
            Assert.Equal("true", option.GetAttribute("aria-selected"));

            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Tab" });

            Assert.Equal("@bob", value);
        }

        [Fact]
        public void ExposesComboboxAriaAndActiveDescendant()
        {
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@a");
            var textarea = cut.Find("textarea");
            var listbox = cut.Find("[role='listbox']");
            var option = cut.Find("[role='option']");

            Assert.Equal("combobox", textarea.GetAttribute("role"));
            Assert.Equal("list", textarea.GetAttribute("aria-autocomplete"));
            Assert.Equal("listbox", textarea.GetAttribute("aria-haspopup"));
            Assert.Equal("true", textarea.GetAttribute("aria-expanded"));
            Assert.Equal(listbox.ParentElement.Id, textarea.GetAttribute("aria-controls"));
            Assert.Equal(option.Id, textarea.GetAttribute("aria-activedescendant"));
            Assert.Equal("true", option.GetAttribute("aria-selected"));
        }

        [Fact]
        public void TextareaAttributesAndFormAriaAreConnected()
        {
            var cut = Render<ElForm>(parameters => parameters
                .AddChildContent<ElFormItem<string>>(item => item
                    .Add(x => x.Name, "assignee")
                    .Add(x => x.Label, "Assignee")
                    .Add(x => x.Required, true)
                    .AddChildContent<ElMention>(mention => mention
                        .Add(x => x.Value, string.Empty)
                        .Add(x => x.Placeholder, "Mention someone")
                        .Add(x => x.Rows, 4)
                        .Add(x => x.Autocomplete, "off")
                        .Add(x => x.Tabindex, 2)
                        .Add(x => x.AriaLabel, "Assignee mention")
                        .Add(x => x.InputStyle, "min-height:48px"))));

            cut.InvokeAsync(() => cut.Instance.Validate());
            cut.Render();

            var label = cut.Find("label.el-form-item__label");
            var textarea = cut.Find("textarea");

            Assert.Equal(textarea.Id, label.GetAttribute("for"));
            Assert.Equal("assignee", textarea.GetAttribute("name"));
            Assert.Equal("Mention someone", textarea.GetAttribute("placeholder"));
            Assert.Equal("4", textarea.GetAttribute("rows"));
            Assert.Equal("off", textarea.GetAttribute("autocomplete"));
            Assert.Equal("2", textarea.GetAttribute("tabindex"));
            Assert.Equal("Assignee mention", textarea.GetAttribute("aria-label"));
            Assert.Equal("true", textarea.GetAttribute("aria-invalid"));
            Assert.Equal("assignee-error", textarea.GetAttribute("aria-describedby"));
            Assert.Equal("min-height:48px", textarea.GetAttribute("style"));
        }

        [Fact]
        public void DoesNotTriggerInsideWordsOrAfterWhitespaceInToken()
        {
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 5, 5 });
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("hi@a");
            Assert.Empty(cut.FindAll(".el-select-dropdown__item"));

            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 9, 9 });
            cut.Find("textarea").Input("@alice hi");

            Assert.Empty(cut.FindAll(".el-select-dropdown__item"));
        }

        [Fact]
        public void ArrowDownUsesCurrentCaretToOpenSuggestions()
        {
            string value = "Hello @a";
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 8, 8 });
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "ArrowDown" });

            Assert.Single(cut.FindAll(".el-select-dropdown__item"));
        }

        [Fact]
        public void PreferLongestPrefixWhenPrefixesOverlap()
        {
            string value = null;
            JSInterop.Setup<int[]>("elementMentionGetSelection", _ => true).SetResult(new[] { 4, 4 });
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Prefixes, new[] { "@", "@@" })
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@@a");
            cut.Find("textarea").KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal("@@alice", value);
        }

        [Fact]
        public void SuggestionItemTemplateAliasRendersCustomContent()
        {
            RenderFragment<MentionOption> template = item => builder =>
            {
                builder.OpenElement(0, "strong");
                builder.AddAttribute(1, "class", "mention-template");
                builder.AddContent(2, $"{item.Label}:{item.Value}");
                builder.CloseElement();
            };

            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.SuggestionItemTemplate, template)
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@a");

            Assert.Contains("mention-template", cut.Markup);
            Assert.Contains("Alice:alice", cut.Markup);
        }

        [Fact]
        public void ClearableClearsMentionValue()
        {
            string value = "hello @alice";
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.Clearable, true)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Options, new List<MentionOption>()));

            cut.Find(".el-mention__clear").Click();

            Assert.Equal(string.Empty, value);
        }

        [Fact]
        public void DisabledMentionOptionDoesNotChangeValue()
        {
            string value = "@a";
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Value, value)
                .Add(x => x.ValueChanged, next => value = next)
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice", Disabled = true }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@a");
            cut.Find(".el-select-dropdown__item").Click();

            Assert.Equal("@a", value);
        }

        [Fact]
        public void DebounceZeroRefreshesSuggestionsImmediately()
        {
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Debounce, 0)
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                }));

            cut.Find("textarea").Input("@a");

            Assert.Single(cut.FindAll(".el-select-dropdown__item"));
        }

        [Fact]
        public void PositionTopRendersDropdownAboveTextarea()
        {
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Position, "top")
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@a");

            var dropdown = cut.Find(".el-mention-dropdown");
            Assert.Contains("el-mention-dropdown--top", dropdown.ClassList);
            // top 模式下下拉渲染在 textarea 之前（Position 被真实消费）
            Assert.True(cut.Markup.IndexOf("el-mention-dropdown--top", StringComparison.Ordinal)
                        < cut.Markup.IndexOf("<textarea", StringComparison.Ordinal));
        }

        [Fact]
        public void PopperClassAppliedToDropdown()
        {
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.PopperClass, "my-custom-popper")
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                })
                .Add(x => x.Debounce, 0));

            cut.Find("textarea").Input("@a");

            Assert.Contains("my-custom-popper", cut.Find(".el-mention-dropdown").ClassList);
        }

        [Fact]
        public void EditableFalseMakesTextareaReadonly()
        {
            var cut = Render<ElMention>(parameters => parameters
                .Add(x => x.Editable, false)
                .Add(x => x.Options, new[]
                {
                    new MentionOption { Label = "Alice", Value = "alice" }
                }));

            Assert.True(cut.Find("textarea").HasAttribute("readonly"));

            var editable = Render<ElMention>(parameters => parameters
                .Add(x => x.Editable, true)
                .Add(x => x.Options, new[] { new MentionOption { Label = "Alice", Value = "alice" } }));

            Assert.False(editable.Find("textarea").HasAttribute("readonly"));
        }
    }
}
