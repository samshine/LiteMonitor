using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using LiteMonitor.src.Core;
using LiteMonitor.src.UI.Controls; // 引用上面新建的控件

namespace LiteMonitor.src.UI.SettingsPage
{
    public class MonitorPage : SettingsPageBase
    {
        private Panel _container;
        private bool _isLoaded = false;

        public MonitorPage()
        {
            // 复用基类和LiteUI颜色
            this.BackColor = UIColors.MainBg;
            
            // 初始化表头 (这里代码比较简单，直接写或简单封装皆可)
            InitHeader(); 
            
            _container = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(20, 5, 20, 20)
            };
            this.Controls.Add(_container);
            // 记得 BringToFront 头部
            this.Controls.SetChildIndex(_container, 0); 
        }

        private void InitHeader()
        {
            var header = new Panel { Dock = DockStyle.Top, Height = 35, BackColor = UIColors.MainBg };
            header.Padding = new Padding(20, 0, 20, 0);
            
            // 使用 MonitorLayout 常量确保对齐
            AddHeadLabel(header, "监控项 (ID)", MonitorLayout.X_ID);
            AddHeadLabel(header, "名称 (Name)", MonitorLayout.X_NAME);
            AddHeadLabel(header, "简称 (Short)", MonitorLayout.X_SHORT);
            AddHeadLabel(header, "显示 / 隐藏 (Show / Hide)", MonitorLayout.X_PANEL); 
            AddHeadLabel(header, "排序 (Sort)", MonitorLayout.X_SORT);
            
            this.Controls.Add(header);
            header.BringToFront();
        }
        
        private void AddHeadLabel(Panel p, string t, int x)
        {
             p.Controls.Add(new Label {
                Text = t, Location = new Point(x + 20, 10), AutoSize = true,
                ForeColor = UIColors.TextSub, Font = UIFonts.Bold(8F)
            });
        }

        public override void OnShow()
        {
            if (Config == null || _isLoaded) return; 

            _container.SuspendLayout();
            _container.Controls.Clear();

            var allItems = Config.MonitorItems.OrderBy(x => x.SortIndex).ToList();
            var groups = allItems.GroupBy(x => x.Key.Split('.')[0]);

            // 倒序添加以配合 Dock = Top 的堆叠顺序
            foreach (var g in groups.Reverse())
            {
                CreateGroupBlock(g.Key, g.ToList());
            }
            _container.ResumeLayout();
            _isLoaded = true;
        }

        private void CreateGroupBlock(string groupKey, List<MonitorItemConfig> items)
        {
            // 1. 外层 Wrapper (用于整体移动)
            var wrapper = new Panel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 20) };
            
            // 2. 卡片背景 (复用 LiteCard)
            var card = new LiteCard { Dock = DockStyle.Top };

            // 3. 内容容器 (行容器)
            var rowsPanel = new Panel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.White };

            // 4. 分组头 (使用新封装的控件)
            string alias = Config.GroupAliases.ContainsKey(groupKey) ? Config.GroupAliases[groupKey] : "";
            var header = new MonitorGroupHeader(groupKey, alias);
            
            // 绑定组排序
            header.MoveUp += (s, e) => MoveControl(wrapper, -1);
            header.MoveDown += (s, e) => MoveControl(wrapper, 1);

            // 5. 循环添加行 (倒序)
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var row = new MonitorItemRow(items[i]);
                row.MoveUp += (s, e) => MoveControl(row, -1);
                row.MoveDown += (s, e) => MoveControl(row, 1);
                rowsPanel.Controls.Add(row);
            }

            // 组装
            card.Controls.Add(rowsPanel);
            card.Controls.Add(header); // Header 最后加，在最上面
            wrapper.Controls.Add(card);
            _container.Controls.Add(wrapper);
        }

        // 通用移动逻辑
        private void MoveControl(Control c, int dir)
        {
            var p = c.Parent;
            int idx = p.Controls.GetChildIndex(c);
            int newIdx = idx - dir;
            // 简单边界检查
            if (newIdx >= 0 && newIdx < p.Controls.Count) 
                p.Controls.SetChildIndex(c, newIdx);
        }

        public override void Save()
        {
            if (!_isLoaded) return;
            
            var flatList = new List<MonitorItemConfig>();
            int sortIndex = 0;

            // 遍历逻辑：从界面读取数据
            // 注意：因为 Dock=Top，Controls[0] 是视觉上的最底部（或者最顶部，取决于你怎么理解）
            // 这里建议：直接递归找 MonitorItemRow
            
            // 简单的做法是把 wrapper 收集到一个列表里再反转遍历
            var wrappers = new List<Control>();
            foreach(Control c in _container.Controls) wrappers.Add(c);
            wrappers.Reverse(); // 变回视觉上的从上到下

            foreach (var wrapper in wrappers)
            {
                var card = wrapper.Controls[0] as LiteCard;
                if (card == null) continue;
                
                // 获取 Header 保存别名
                var header = card.Controls.OfType<MonitorGroupHeader>().FirstOrDefault();
                if (header != null)
                {
                    string alias = header.InputAlias.Inner.Text.Trim();
                    if (!string.IsNullOrEmpty(alias)) Config.GroupAliases[header.GroupKey] = alias;
                    else Config.GroupAliases.Remove(header.GroupKey);
                }

                // 获取行容器
                var rowsPanel = card.Controls.OfType<Panel>().FirstOrDefault(p => !(p is MonitorGroupHeader));
                if (rowsPanel != null)
                {
                    // 同理，行也是 Dock Top，需要反转
                    var rows = rowsPanel.Controls.Cast<MonitorItemRow>().Reverse().ToList();
                    
                    foreach (var row in rows)
                    {
                        // ★ 调用 Row 自己的保存逻辑
                        row.SyncToConfig();
                        row.Config.SortIndex = sortIndex++;
                        flatList.Add(row.Config);
                    }
                }
            }

            Config.MonitorItems = flatList;
            
            // 应用更改
            Config.SyncToLanguage();
            AppActions.ApplyMonitorLayout(UI, MainForm);
        }
    }
}