using System;

namespace AutoAudioDemo.Utils
{
    /// <summary>
    /// 通用枚举解析工具类
    /// </summary>
    public static class EnumUtils
    {
        /// <summary>
        /// 安全地解析枚举值，如果解析失败则返回默认值
        /// </summary>
        /// <typeparam name="TEnum">枚举类型</typeparam>
        /// <param name="value">要解析的字符串值</param>
        /// <param name="fallback">解析失败时的默认值</param>
        /// <returns>解析后的枚举值或默认值</returns>
        public static TEnum ParseEnum<TEnum>(string value, TEnum fallback) where TEnum : struct
        {
            if (!string.IsNullOrEmpty(value) && Enum.TryParse<TEnum>(value, out var parsed))
            {
                return parsed;
            }
            return fallback;
        }
        
        /// <summary>
        /// 解析布尔值
        /// </summary>
        /// <param name="s">字符串值</param>
        /// <returns>布尔值</returns>
        public static bool ParseBool(string s)
        {
            return !string.IsNullOrEmpty(s) && (s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1");
        }
        
        /// <summary>
        /// 解析整数值
        /// </summary>
        /// <param name="s">字符串值</param>
        /// <returns>整数值，解析失败返回0</returns>
        public static int ParseInt(string s)
        {
            int v; 
            if (int.TryParse(s, out v)) return v; 
            return 0;
        }
        
        /// <summary>
        /// 解析浮点数值并限制在0-1范围内
        /// </summary>
        /// <param name="s">字符串值</param>
        /// <returns>限制在0-1范围内的浮点数值</returns>
        public static float Clamp01(string s)
        {
            float v; 
            if (float.TryParse(s, out v)) return UnityEngine.Mathf.Clamp01(v); 
            return 0f;
        }
    }
}
