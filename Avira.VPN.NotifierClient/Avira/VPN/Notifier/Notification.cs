using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Avira.VPN.Notifier
{
    public class Notification
    {
        public enum TemplateType
        {
            Template0,
            Template1,
            Template2,
            Auto,
            FtuTemplate,
            CustomTemplate
        }

        public enum IconType
        {
            Alert,
            Check,
            Feedback,
            Default
        }

        public enum PositionType
        {
            Default,
            CenterScreen
        }

        public enum PriorityLevel
        {
            Low,
            Normal,
            High
        }

        public class Command
        {
            [JsonProperty(PropertyName = "Id")] public string Id { get; set; }

            [JsonProperty(PropertyName = "Text")] public string Text { get; set; }

            [JsonIgnore] public Action<string, string, string> Run { get; set; }

            public Command()
            {
            }

            public Command(string id, string text = null)
            {
                Id = id;
                Text = text;
            }
        }

        public class FtuPage
        {
            [JsonProperty(PropertyName = "Header")]
            public string Header { get; set; }

            [JsonProperty(PropertyName = "Text")] public string Text { get; set; }

            [JsonProperty(PropertyName = "Image")] public string Image { get; set; }

            [JsonProperty(PropertyName = "Checkbox")]
            public string Checkbox { get; set; }

            [JsonProperty(PropertyName = "Button")]
            public string Button { get; set; }
        }

        [JsonProperty(PropertyName = "UniqueId")]
        public int UniqueId { get; set; }

        [JsonProperty(PropertyName = "Id")] public string Id { get; set; }

        [JsonProperty(PropertyName = "Priority")]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Normal;


        [JsonProperty(PropertyName = "Title")] public string Title { get; set; }

        [JsonProperty(PropertyName = "Title2")]
        public string Title2 { get; set; }

        [JsonProperty(PropertyName = "Question")]
        public string Question { get; set; }

        [JsonProperty(PropertyName = "Message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "Hint")] public string Hint { get; set; }

        [JsonProperty(PropertyName = "Image")] public string Image { get; set; }

        [JsonProperty(PropertyName = "Action1")]
        public Command Action1 { get; set; }

        [JsonProperty(PropertyName = "Action2")]
        public Command Action2 { get; set; }

        [JsonProperty(PropertyName = "Unregistered")]
        public bool TrialDisabled { get; set; }

        [JsonProperty(PropertyName = "Timeout")]
        public int Timeout { get; set; } = 30000;


        [JsonProperty(PropertyName = "Ftu")] public List<FtuPage> Ftu { get; set; }

        [JsonProperty(PropertyName = "Template")]
        public TemplateType Template { get; set; } = TemplateType.Auto;


        [JsonProperty(PropertyName = "TemplateName")]
        public string TemplateName { get; set; } = "";


        [JsonProperty(PropertyName = "Icon")] public IconType Icon { get; set; } = IconType.Default;


        [JsonProperty(PropertyName = "Position")]
        public PositionType Position { get; set; }

        [JsonProperty(PropertyName = "OnlyIfNoForegroundUiWindow")]
        public bool OnlyIfNoForegroundUiWindow { get; set; }

        [JsonProperty(PropertyName = "IsMovable")]
        public bool IsMovable { get; set; }

        [JsonProperty(PropertyName = "Close2")]
        public string Close2 { get; set; }
    }
}