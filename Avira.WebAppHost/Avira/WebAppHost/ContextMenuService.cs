using System;
using System.Windows.Forms;
using Avira.Win.Messaging;
using Newtonsoft.Json.Linq;

namespace Avira.WebAppHost
{
    public class ContextMenuService
    {
        private readonly ContextMenuStrip contextMenu;

        private readonly object contextMenuLock = new object();

        public event EventHandler<EventArgs<string>> ItemClicked;

        public ContextMenuService(ContextMenuStrip contextMenu)
        {
            this.contextMenu = contextMenu;
        }

        public void Add(JObject menuInfo)
        {
            lock (contextMenuLock)
            {
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem
                {
                    Name = menuInfo["name"]!.ToString(),
                    Text = menuInfo["text"]!.ToString()
                };
                toolStripMenuItem.Click += delegate(object sender, EventArgs args)
                {
                    this.ItemClicked?.Invoke(sender, new EventArgs<string>(((ToolStripMenuItem)sender).Name));
                };
                if (menuInfo["enabled"] != null)
                {
                    toolStripMenuItem.Enabled = (bool)menuInfo["enabled"];
                }

                if (menuInfo["index"] == null || contextMenu.Items.Count < (int)menuInfo["index"])
                {
                    contextMenu.Items.Add(toolStripMenuItem);
                }
                else
                {
                    contextMenu.Items.Insert((int)menuInfo["index"], toolStripMenuItem);
                }
            }
        }

        public void Set(JObject menuInfo)
        {
            ToolStripItem[] array = contextMenu.Items.Find(menuInfo["name"]!.ToString(), searchAllChildren: false);
            if (array.Length != 0)
            {
                ToolStripItem toolStripItem = array[0];
                if (menuInfo["text"] != null)
                {
                    toolStripItem.Text = menuInfo["text"]!.ToString();
                }

                if (menuInfo["enabled"] != null)
                {
                    toolStripItem.Enabled = (bool)menuInfo["enabled"];
                }
            }
        }
    }
}