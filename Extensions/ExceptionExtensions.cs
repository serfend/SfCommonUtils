using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Extensions
{

	public static class ExceptionExtensions
	{
		public static string? ToSummary(this AggregateException? ex)
		{
			if (ex == null) return null;
			var innerExceptions = ex.InnerExceptions;
			var inner = innerExceptions.ToList().Select((i,index) => i.ToSummary(index!=0));
			var result = $"{(ex as Exception).ToSummary(false)}\n\n{Enumerable.Repeat('-', 10)}{string.Join("\n\n", inner)}";
			return result;
		}
		public static string ToSummary(this Exception? ex, bool nestView = true)
		{
			if (ex == null) return "无异常";
			var content = $"{ex.Message}\n{ex.StackTrace}";
			if (!nestView)
				return content;
			var e = (ex as AggregateException);
			var result = e?.ToSummary() ??
			 content;
			return result;
		}
	}
}
