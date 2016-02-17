using System;
using MugenMvvmToolkit.Modules;
using MugenMvvmToolkit.Interfaces.Models;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit;


namespace Qmunicate.Xamarin
{
	public class PortableModule : ModuleBase
	{
		public PortableModule() : base(false, LoadMode.All)
		{
		}

		protected override bool LoadInternal ()
		{
			IocContainer.BindToConstant<IViewModelSettings> (new DefaultViewModelSettings {
				DefaultBusyMessage = "Loading..."
			});

			return true;
		}

		protected override void UnloadInternal ()
		{
			
		}
	}
}

