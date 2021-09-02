using System;

namespace Hecate {
	public static class MainClass {
		public static void Main(string[] args) {
			// Initialize generator
			StoryGenerator generator = new StoryGenerator();

			if (args.Length < 1) {
				throw new Exception("USAGE: Hecate <data dir>");
			}
			generator.ParseRuleDirectory(args[0]);

			Console.Out.WriteLine(generator.Generate("[=>story]"));
		}
	}
}
