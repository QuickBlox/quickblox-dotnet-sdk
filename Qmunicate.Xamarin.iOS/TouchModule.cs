using MugenMvvmToolkit.Modules;
using MugenMvvmToolkit.Models;
using MugenMvvmToolkit;

namespace Qmunicate.Xamarin.iOS
{
	public class TouchModule : ModuleBase
	{
		public TouchModule () : base(false, LoadMode.Runtime)
		{
		}

		protected override bool LoadInternal ()
		{
			//IocContainer.BindToConstant(

			IocContainer.BindToConstant<IDeviceUid> (new IOSDeviceUid ());
			return true;
		}

		protected override void UnloadInternal ()
		{
			
		}
	}
}

