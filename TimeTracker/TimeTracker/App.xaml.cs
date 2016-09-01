using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using JustObjectsPrototype.Universal;
using JustObjectsPrototype.Universal.JOP;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TimeTracker
{
	sealed partial class App : Application
	{
		public App()
		{
			this.InitializeComponent();
			Suspending += App_Suspending;
		}

		private void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
		{
			Prototype.Remember(typeof(Stunden), typeof(Minuten), typeof(Datum));
		}

		protected async override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Prototype = Show.Prototype(
				With.Remembered(Einstellungen.Alle)
				.AndViewOf<Zeiteintrag>()
				.AndViewOf<Tätigkeit>()
				.AndViewOf<Projekt>()
				.AndViewOf<Kunde>()
				.AndViewOf<Einstellungen>()
				.AndOpen<Zeiteintrag>());
		}

		public static Prototype Prototype;
	}
















	[Icon(Symbol.Folder), Title("Projekte")]
	public class Projekt
	{
		public string Name { get; set; }
		string kürzel;
		public string Kürzel { get { return kürzel; } set { kürzel = value?.ToUpper(); } }

		public int Erfasste_Zeiten => App.Prototype.Repository.OfType<Zeiteintrag>().Count(z => z.Tokens.Contains(this));

		[Icon(Symbol.Add), JumpsToResult]
		public async static Task<Projekt> Neu()
		{
			var projekt = new Projekt();
			return projekt;
		}

		[Icon(Symbol.Remove), RequiresConfirmation]
		public async void Löschen(ObservableCollection<Projekt> projekte)
		{
			projekte.Remove(this);
		}

		public override string ToString() => $"{Name}";
	}


	[Icon(Symbol.Flag), Title("Tätigkeiten")]
	public class Tätigkeit
	{
		public string Bezeichnung { get; set; }
		string kürzel;
		public string Kürzel { get { return kürzel; } set { kürzel = value?.ToUpper(); } }

		[Icon(Symbol.Add), JumpsToResult]
		public async static Task<Tätigkeit> Neu()
		{
			var tätigkeit = new Tätigkeit();
			return tätigkeit;
		}

		[Icon(Symbol.Remove), RequiresConfirmation]
		public async void Löschen(ObservableCollection<Tätigkeit> tätigkeiten)
		{
			tätigkeiten.Remove(this);
		}

		public override string ToString() => $"{Bezeichnung}";
	}


	[Icon(Symbol.Contact2), Title("Kunden")]
	public class Kunde
	{
		public string Vorname { get; set; }
		public string Nachname { get; set; }
		string kürzel;
		public string Kürzel { get { return kürzel; } set { kürzel = value?.ToUpper(); } }

		public int Erfasste_Zeiten => App.Prototype.Repository.OfType<Zeiteintrag>().Count(z => z.Tokens.Contains(this));

		[Icon(Symbol.Add), JumpsToResult]
		public async static Task<Kunde> Neu()
		{
			var kunde = new Kunde();
			return kunde;
		}

		[Icon(Symbol.Remove), RequiresConfirmation]
		public async void Löschen(ObservableCollection<Kunde> kunden)
		{
			kunden.Remove(this);
		}

		public override string ToString() => $"{Vorname} {Nachname}";
	}


	[Icon(Symbol.Clock), Title("Zeiteinträge")]
	public class Zeiteintrag
	{
		[Editor(hide: true)]
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Beschreibung { get; set; }

		[Editor(@readonly: true)]
		public List<object> Tokens { get; set; }

		[Icon(Symbol.Add)]
		public async static Task<Zeiteintrag> Neu(string beschreibung,
			Tätigkeit Tätigkeit,
			Projekt Projekt,
			Kunde Kunde,
			ObservableCollection<Tätigkeit> tätigkeiten,
			ObservableCollection<Projekt> projekte,
			ObservableCollection<Kunde> kunden)
		{
			if (string.IsNullOrEmpty(beschreibung))
			{
				await Show.Message("Beschreibung angeben.");
				return null;
			}
			else
			{
				var tätigkeitenNachKürzel = tätigkeiten.ToLookup(t => t.Kürzel);
				var projekteNachKürzel = projekte.ToLookup(p => p.Kürzel);
				var kundenNachKürzel = kunden.ToLookup(k => k.Kürzel);

				var tokens = beschreibung.ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).SelectMany(t =>
				{
					var menge = t.Substring(0, t.Length - 1);
					decimal parsed;
					if (decimal.TryParse(t, out parsed))
					{
						if (parsed < 10)
						{
							return new[] { new Stunden { Menge = parsed } };
						}
						else
						{
							return new[] { new Minuten { Menge = parsed } };
						}
					}
					else if (decimal.TryParse(menge, out parsed))
					{
						if (t.EndsWith("H"))
						{
							return new[] { new Stunden { Menge = parsed } };
						}
						else if (t.EndsWith("M"))
						{
							return new[] { new Minuten { Menge = parsed } };
						}
						else
						{
							return Enumerable.Empty<object>();
						}
					}
					else if (t.All(c => c == '-'))
					{
						return new[] { new Datum { Tag = DateTime.Today.AddDays(-t.Length) } };
					}
					else if (t.All(c => c == '+'))
					{
						return new[] { new Datum { Tag = DateTime.Today.AddDays(t.Length) } };
					}
					else if (tätigkeitenNachKürzel.Contains(t))
					{
						return tätigkeitenNachKürzel[t];
					}
					else if (projekteNachKürzel.Contains(t))
					{
						return projekteNachKürzel[t];
					}
					else if (kundenNachKürzel.Contains(t))
					{
						return kundenNachKürzel[t];
					}
					else
					{
						return Enumerable.Empty<object>();
					}
				}).ToList();

				var zeiteintrag = new Zeiteintrag
				{
					Beschreibung = beschreibung.Trim(),
					Tokens = tokens
						.Concat(tokens.OfType<Datum>().Any() ? Enumerable.Empty<Datum>() : new[] { new Datum { Tag = DateTime.Today } })
						.Concat(new object[] { Tätigkeit, Projekt, Kunde })
						.ToList()
				};

				return zeiteintrag;
			}
		}

		[Icon(Symbol.Remove), RequiresConfirmation]
		public async void Löschen(ObservableCollection<Zeiteintrag> zeiten)
		{
			zeiten.Remove(this);
		}
	}

	public class Stunden
	{
		public decimal Menge { get; set; }
		public override string ToString() => $"{Menge} Stunden";
	}
	public class Minuten
	{
		public decimal Menge { get; set; }
		public override string ToString() => $"{Menge} Minuten";
	}
	public class Datum
	{
		public DateTime Tag { get; set; }
		public override string ToString() => $"am {Tag.ToString("dd.MM.yyyy")}";
	}

















	[Icon(Symbol.Setting), DataContract]
	public abstract class Einstellungen
	{
		public static Einstellungen[] Alle => new Einstellungen[] { new AllgemeineEinstellungen(), new SpeicherEinstellungen() };

		[DataContract]
		public class SpeicherEinstellungen : Einstellungen
		{
			public override string ToString() => "Speicher";

			public IEnumerable<object> Gespeicherte_Objekte => App.Prototype.Repository.Where(o => !(o is Einstellungen));

			[Icon(Symbol.Delete)]
			public async Task Alles_Löschen()
			{
				App.Prototype.Repository.Clear();
			}
		}

		[DataContract]
		public class AllgemeineEinstellungen : Einstellungen
		{
			public override string ToString() => "Allgemein";

			[DataMember]
			public string Value1 { get; set; }

			[DataMember]
			public string Value2 { get; set; }
		}
	}
}

