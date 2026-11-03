using System;
using System.Linq;

namespace II.RPA.Plugin.JavaAlter.Agent
{
    internal static class BackgroundTextInput
    {
        internal static void SetText(JavaObjectWrapper target, string text, bool replace)
        {
            if (target == null)
                throw new InvalidOperationException("未找到 Java 文本控件。");
            if (target.Tech != JavaTech.Awt)
                throw new NotSupportedException("后台输入暂时仅支持标准 Swing/AWT 文本控件。");

            // Inspect the real Java hierarchy, rather than a selector's class name.
            var hierarchy = Invoke(target, "#LINEAGE").Skip(1).OfType<string>().ToArray();
            var supported = hierarchy.Any(name => name == "javax.swing.JTextField"
                || name == "javax.swing.JTextArea" || name == "java.awt.TextField"
                || name == "java.awt.TextArea");
            if (!supported || hierarchy.Contains("javax.swing.JFormattedTextField"))
                throw new NotSupportedException("后台输入暂时仅支持普通 Swing/AWT 文本框，格式化控件需单独适配。");
            if (!ReadValue<bool>(target, "isEnabled") || !ReadValue<bool>(target, "isEditable"))
                throw new InvalidOperationException("Java 文本控件已禁用或不可编辑。");

            var expected = text ?? string.Empty;
            if (!replace)
                expected = ReadValue<string>(target, "getText") + expected;

            // The agent returns null for both a void result and some invocation errors.
            // Always read back, even when the caller disables optional verification.
            Invoke(target, "setText", expected);
            if (!string.Equals(ReadValue<string>(target, "getText"), expected, StringComparison.Ordinal))
                throw new InvalidOperationException("Java 后台输入后的文本与预期不一致。");
        }

        private static T ReadValue<T>(JavaObjectWrapper target, string method)
        {
            var result = Invoke(target, method);
            if (result.Length != 1 || !(result[0] is T))
                throw new InvalidOperationException($"无法读取 Java 文本控件的 {method} 结果。");
            return (T)result[0];
        }

        private static object[] Invoke(JavaObjectWrapper target, string method, params object[] args)
        {
            var arguments = new object[args.Length + 2];
            arguments[0] = target;
            arguments[1] = method;
            Array.Copy(args, 0, arguments, 2, args.Length);
            var result = target.Proxy.CallFunction(target.Tech, FunctionId.FuncInvoke, arguments, out CallStatus status);
            if (status != CallStatus.Ok || result == null)
                throw new InvalidOperationException($"Java 文本控件调用 {method} 失败，状态：{status}。");
            return result;
        }
    }
}
