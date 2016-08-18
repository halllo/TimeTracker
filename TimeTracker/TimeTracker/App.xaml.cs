using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using JustObjectsPrototype.Universal;
using JustObjectsPrototype.Universal.JOP;
using Windows.ApplicationModel.Activation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TimeTracker
{
	sealed partial class App : Application
	{
		public App()
		{
			this.InitializeComponent();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			var objects = new ObservableCollection<object>
			{
				new Projekt { Name = "Erstes Projekt" },
				new Projekt { Name = "Große Akte" },
				new Projekt { Name = "Sonstiges" },
				new Kunde { Vorname = "Manuel", Nachname = "Naujoks"},
			};

			Show.Prototype(With.These(objects)
				.AndViewOf<Zeiteintrag>()
				.AndViewOf<Projekt>()
				.AndViewOf<Kunde>()
				.AndOpen<Zeiteintrag>());
		}
	}
















	[Icon(Symbol.Folder)]
	class Projekt
	{
		public string Name { get; set; }
		public string Kürzel { get; set; }

		[Icon(Symbol.Add), JumpToResult]
		public static Projekt Neu()
		{
			return new Projekt();
		}

		[Icon(Symbol.Remove)]
		public void Löschen(ObservableCollection<Projekt> projekte)
		{
			projekte.Remove(this);
		}
	}

	[Icon(Symbol.Contact2)]
	class Kunde
	{
		public string Vorname { get; set; }
		public string Nachname { get; set; }
		public string Kürzel { get; set; }

		[Icon(Symbol.Add), JumpToResult]
		public static Kunde Neu()
		{
			return new Kunde();
		}

		[Icon(Symbol.Remove)]
		public void Löschen(ObservableCollection<Kunde> kunden)
		{
			kunden.Remove(this);
		}
	}

	[Icon(Symbol.Clock)]
	class Zeiteintrag
	{
		public string Beschreibung { get; set; }

		[Editor(@readonly: true)]
		public Kunde Kunde { get; set; }

		[Editor(@readonly: true)]
		public Projekt Projekt { get; set; }

		[Icon(Symbol.Add)]
		public async static Task<Zeiteintrag> Neu(string beschreibung, Projekt projekt, Kunde kunde)
		{
			if (string.IsNullOrEmpty(beschreibung))
			{
				await new MessageDialog("Beschreibung angeben.").ShowAsync();
				return null;
			}
			else
			{
				return new Zeiteintrag
				{
					Beschreibung = beschreibung,
					Projekt = projekt,
					Kunde = kunde
				};
			}
		}

		[Icon(Symbol.Remove)]
		public async void Löschen(ObservableCollection<Zeiteintrag> zeiten)
		{
			var löschDialog = new MessageDialog($"Zeiteintrag \"{Beschreibung}\" löschen?");
			löschDialog.Options = MessageDialogOptions.None;
			löschDialog.Commands.Add(new UICommand("Ja", async cmd =>
			{
				zeiten.Remove(this);
			}));
			löschDialog.Commands.Add(new UICommand("Nein", cmd => { }));
			löschDialog.CancelCommandIndex = 1;
			löschDialog.DefaultCommandIndex = 0;

			await löschDialog.ShowAsync();
		}
	}
}
