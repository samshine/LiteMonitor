using System.Windows.Forms;
using System.Drawing;
using LiteMonitor.src.Core;

namespace LiteMonitor.src.UI.SettingsPage
{
    public interface ISettingsPage
    {
        void Save();
        void OnShow();
    }

    public class SettingsPageBase : UserControl, ISettingsPage
    {
        protected Settings Config;
        protected MainForm MainForm;     // ★ 新增：主窗口引用
        protected UIController UI;       // ★ 新增：UI控制器引用
        
        // 定义全局通用的淡灰背景色，方便统一修改
        public static readonly Color GlobalBackColor = Color.FromArgb(249, 249, 249); 

        public SettingsPageBase() 
        {
            // 改为淡灰，不再是刺眼的纯白
            this.BackColor = GlobalBackColor; 
            this.Dock = DockStyle.Fill;
        }

        // ★ 修改：接收更多上下文
        public void SetContext(Settings cfg, MainForm form, UIController ui)
        {
            Config = cfg;
            MainForm = form;
            UI = ui;
        }

        // 为了兼容旧代码，保留 SetConfig 但在内部转发（可选）
        // public void SetConfig(Settings cfg) 
        // {
        //     Config = cfg;
        // }

        public virtual void Save() { }
        public virtual void OnShow() { }
    }
}