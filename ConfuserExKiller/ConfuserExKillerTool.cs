using System.IO;
using System.Linq;
using System.Reflection;
using ConfuserExTools.AntiTamperKiller;
using ConfuserExTools.ConstantKiller;
using ConfuserExTools.ProxyKiller;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Tool;
using Tool.Interface;

namespace ConfuserExTools.ConfuserExKiller {
	public sealed class ConfuserExKillerTool : ITool<ConfuserExKillerSettings> {
		public string Title => GetTitle();

		public void Execute(ConfuserExKillerSettings settings) {
			Logger.Initialize(false);
			var reflModule = Assembly.LoadFile(settings.AssemblyPath).ManifestModule;
			byte[] peImageOld = File.ReadAllBytes(settings.AssemblyPath);
			byte[] peImage = AntiTamperKillerImpl.Execute(reflModule, peImageOld);
			if (!peImage.SequenceEqual(peImageOld))
				Logger.LogInfo($"AntiTamper已移除");
			using (var module = ModuleDefMD.Load(peImage)) {
				int count = ProxyKillerImpl.Execute(module, false, true);
				Logger.LogInfo($"共 {count} 个代理方法被还原");
				count = ConstantKillerImpl.Execute(module, reflModule);
				Logger.LogInfo($"共 {count} 个常量被解密");
				string newFilePath = PathInsertPostfix(settings.AssemblyPath, ".cexk");
				Logger.LogInfo($"正在保存: {newFilePath}");
				Logger.LogInfo();
				module.Write(newFilePath, new ModuleWriterOptions(module) { Logger = DnlibLogger.Instance });
			}
			Logger.Flush();
		}

		private static string PathInsertPostfix(string path, string postfix) {
			return Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + postfix + Path.GetExtension(path));
		}

		private static string GetTitle() {
			string productName = GetAssemblyAttribute<AssemblyProductAttribute>().Product;
			string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			string copyright = GetAssemblyAttribute<AssemblyCopyrightAttribute>().Copyright.Substring(12);
			int firstBlankIndex = copyright.IndexOf(' ');
			string copyrightOwnerName = copyright.Substring(firstBlankIndex + 1);
			string copyrightYear = copyright.Substring(0, firstBlankIndex);
			return $"{productName} v{version} by {copyrightOwnerName} {copyrightYear}";
		}

		private static T GetAssemblyAttribute<T>() {
			return (T)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false)[0];
		}
	}
}
