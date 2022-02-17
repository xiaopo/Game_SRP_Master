
using System.Runtime.InteropServices;
namespace CustomSR
{
    public static class ReinterpretExtensions
    {
        // StructLayout使设计者可以控制类或结构的数据字段的物理布局  
        // Explicit与FieldOffset一起可以控制每个数据成员的精确位置  
        [StructLayout(LayoutKind.Explicit)]
        struct IntFloat
        {
            //FieldOffset控制字段所在的物理位置偏移为0  
            [FieldOffset(0)]
            public int intValue;
            //同样偏移为0，开始位置与intValue重叠了。
            [FieldOffset(0)]
            public float floatValue;
        }

        public static float ReinterpretAsFloat(this int value)
        {
            IntFloat converter = default;
            converter.intValue = value;
            return converter.floatValue;
        }
    }
}
