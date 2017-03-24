using System.Windows;

namespace LocalBlast
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			if (Settings.Default.UpgradeRequired)
			{
				Settings.Default.Upgrade();
				Settings.Default.UpgradeRequired = false;
			}
		}

		protected override void OnExit(ExitEventArgs e)
		{
			Settings.Default.Save();

			base.OnExit(e);
		}
	}
}
