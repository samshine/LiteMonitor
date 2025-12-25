using System;
using System.Drawing;
using System.Windows.Forms;
using LiteMonitor.src.Core;
// 引用 LiteUI 所在的命名空间
using LiteMonitor.src.UI.Controls; 

namespace LiteMonitor.src.UI.Controls
{
    // 1. 布局常量：集中管理坐标，修改一处即可调整所有列宽
    public static class MonitorLayout
    {
        public const int H_ROW = 44;     // 行高
        public const int X_ID = 20;      // ID列起点
        public const int X_NAME = 125;   // Name列起点
        public const int X_SHORT = 245;  // Short列起点
        public const int X_PANEL = 325;  // Panel开关起点
        public const int X_TASKBAR = 415;// Taskbar开关起点
        public const int X_SORT = 500;   // 排序按钮起点
    }

    // 2. 封装“单行监控项”：继承自 Panel，内部组合 LiteUI 组件
    public class MonitorItemRow : Panel
    {
        // 持有数据源，方便保存时直接读取
        public MonitorItemConfig Config { get; private set; }

        // 公开控件供外部访问（如果需要的话），或者直接提供 Get 方法
        private LiteUnderlineInput _inputName;
        private LiteUnderlineInput _inputShort;
        private LiteCheck _chkPanel;
        private LiteCheck _chkTaskbar;

        // 事件暴露
        public event EventHandler MoveUp;
        public event EventHandler MoveDown;

        public MonitorItemRow(MonitorItemConfig item)
        {
            this.Config = item;
            this.Dock = DockStyle.Top;
            this.Height = MonitorLayout.H_ROW;
            this.BackColor = Color.White; // 或 UIColors.CardBg

            // A. ID Label (使用原生Label，因为只需展示)
            var lblId = new Label
            {
                Text = item.Key,
                Location = new Point(MonitorLayout.X_ID, 14),
                Size = new Size(90, 20),
                AutoEllipsis = true,
                ForeColor = UIColors.TextSub, // 复用 LiteUI 颜色
                Font = UIFonts.Regular(8F)    // 复用字体
            };

            // B. Inputs (复用 LiteUnderlineInput)
            // 处理默认值逻辑
            string defName = LanguageManager.T("Items." + item.Key);
            string valName = string.IsNullOrEmpty(item.UserLabel) ? defName : item.UserLabel;
            
            // 注意：这里复用了 LiteUnderlineInput，不用重复写绘制代码
            _inputName = new LiteUnderlineInput(valName, "", "", 100) 
            { Location = new Point(MonitorLayout.X_NAME, 8) };

            string defShortKey = "Short." + item.Key;
            string defShort = LanguageManager.T(defShortKey);
            if (defShort.StartsWith("Short.")) defShort = item.Key.Split('.')[1]; 
            string valShort = string.IsNullOrEmpty(item.TaskbarLabel) ? defShort : item.TaskbarLabel;

            _inputShort = new LiteUnderlineInput(valShort, "", "", 60) 
            { Location = new Point(MonitorLayout.X_SHORT, 8) };

            // C. Checks (复用 LiteCheck)
            _chkPanel = new LiteCheck(item.VisibleInPanel, LanguageManager.T("Menu.MainForm")) 
            { Location = new Point(MonitorLayout.X_PANEL, 10) };
            
            _chkTaskbar = new LiteCheck(item.VisibleInTaskbar, LanguageManager.T("Menu.Taskbar")) 
            { Location = new Point(MonitorLayout.X_TASKBAR, 10) };

            // D. Sort Buttons (复用 LiteSortBtn)
            var btnUp = new LiteSortBtn("▲") { Location = new Point(MonitorLayout.X_SORT, 10) };
            var btnDown = new LiteSortBtn("▼") { Location = new Point(MonitorLayout.X_SORT + 36, 10) };
            
            // 内部绑定事件转发
            btnUp.Click += (s, e) => MoveUp?.Invoke(this, EventArgs.Empty);
            btnDown.Click += (s, e) => MoveDown?.Invoke(this, EventArgs.Empty);

            // 添加所有控件
            this.Controls.AddRange(new Control[] { lblId, _inputName, _inputShort, _chkPanel, _chkTaskbar, btnUp, btnDown });
        }

        // 自绘底部分割线 (复用 UIColors.Border)
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(UIColors.Border))
                e.Graphics.DrawLine(p, MonitorLayout.X_ID, Height - 1, Width - 20, Height - 1);
        }

        // ★ 核心优势：自我管理数据回写逻辑
        public void SyncToConfig()
        {
            // Name
            string valName = _inputName.Inner.Text.Trim();
            string originalName = LanguageManager.GetOriginal("Items." + Config.Key);
            Config.UserLabel = string.Equals(valName, originalName, StringComparison.OrdinalIgnoreCase) ? "" : valName;

            // Short
            string valShort = _inputShort.Inner.Text.Trim();
            string originalShort = LanguageManager.GetOriginal("Short." + Config.Key);
            Config.TaskbarLabel = string.Equals(valShort, originalShort, StringComparison.OrdinalIgnoreCase) ? "" : valShort;

            // Checks
            Config.VisibleInPanel = _chkPanel.Checked;
            Config.VisibleInTaskbar = _chkTaskbar.Checked;
        }
    }

    // 3. 封装“分组头”
    public class MonitorGroupHeader : Panel
    {
        public string GroupKey { get; private set; }
        public LiteUnderlineInput InputAlias { get; private set; }
        public event EventHandler MoveUp;
        public event EventHandler MoveDown;

        public MonitorGroupHeader(string groupKey, string alias)
        {
            this.GroupKey = groupKey;
            this.Dock = DockStyle.Top;
            this.Height = 45;
            this.BackColor = UIColors.GroupHeader; // 复用颜色

            var lblId = new Label { 
                Text = groupKey, 
                Location = new Point(MonitorLayout.X_ID, 12), 
                AutoSize = true, 
                Font = UIFonts.Bold(9F), 
                ForeColor = Color.Gray 
            };

            string defGName = LanguageManager.T("Groups." + groupKey);
            if (defGName.StartsWith("Groups.")) defGName = groupKey;
            
            InputAlias = new LiteUnderlineInput(string.IsNullOrEmpty(alias) ? defGName : alias, "", "", 100) 
            { Location = new Point(MonitorLayout.X_NAME, 8) };
            
            // 特殊样式处理
            InputAlias.SetBg(UIColors.GroupHeader); 
            InputAlias.Inner.Font = UIFonts.Bold(9F);

            var btnUp = new LiteSortBtn("▲") { Location = new Point(MonitorLayout.X_SORT, 10) };
            var btnDown = new LiteSortBtn("▼") { Location = new Point(MonitorLayout.X_SORT + 36, 10) };
            btnUp.Click += (s, e) => MoveUp?.Invoke(this, EventArgs.Empty);
            btnDown.Click += (s, e) => MoveDown?.Invoke(this, EventArgs.Empty);

            this.Controls.AddRange(new Control[] { lblId, InputAlias, btnUp, btnDown });
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using(var p = new Pen(UIColors.Border)) 
                e.Graphics.DrawLine(p, 0, Height-1, Width, Height-1);
        }
    }
}