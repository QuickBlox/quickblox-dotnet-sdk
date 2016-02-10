using System;
using MugenMvvmToolkit.Modules;
using MugenMvvmToolkit.Models;

namespace Qmunicate.Xamarin.iOS
{
	public class TouchModule : ModuleBase
	{
		public TouchModule () : base(false, LoadMode.Runtime)
		{
		}

		protected override bool LoadInternal ()
		{
			//IocContainer.BindToConstant

			return true;
		}

		protected override void UnloadInternal ()
		{
			
		}
	}
}

