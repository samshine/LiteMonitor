using System;
using System.Drawing;

namespace LiteMonitor
{
    public class MetricItem
    {
        public string Key { get; set; } = "";
        public string Label { get; set; } = "";
        public Rectangle Bounds { get; set; } = Rectangle.Empty;

        public float? Value { get; set; } = null;
        public float DisplayValue { get; set; } = 0f;

        /// <summary>
        /// 平滑更新显示值，防止数值突变造成跳动。
        /// </summary>
        /// <param name="speed">
        /// 平滑速度系数（0~1）:
        /// - 1.0 表示瞬时更新（无动画）
        /// - 0.3~0.5 为推荐平滑范围
        ///   值越小 → 越平滑但响应稍慢。
        /// </param>
        public void TickSmooth(double speed)
        {
            // 若当前无有效数值，直接返回
            if (!Value.HasValue) return;

            float target = Value.Value;
            float diff = Math.Abs(target - DisplayValue);

            // 忽略非常微小的变化，防止数值抖动闪烁
            if (diff < 0.05f) return;

            // 若差距过大或速度系数接近 1，直接跳至目标值
            // 例如首次加载、切换硬件时
            if (diff > 15f || speed >= 0.9)
            {
                DisplayValue = target;
            }
            else
            {
                // 核心平滑逻辑：
                // 每帧按 speed 比例逼近目标，使数值逐步过渡
                // 示例：speed = 0.35 → 每次更新向目标推进 35%
                DisplayValue += (float)((target - DisplayValue) * speed);
            }
        }



    }
}
