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
			Prototype.Remember(typeof(Stunden), typeof(Minuten), typeof(Datum), typeof(Trennzeichen));
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

		[Icon(Symbol.Delete), RequiresConfirmation]
		public async static void Alle_Löschen(ObservableCollection<Zeiteintrag> zeiteinträge)
		{
			zeiteinträge.Clear();
		}

		[Icon(Symbol.Remove), RequiresConfirmation]
		public async void Löschen(ObservableCollection<Zeiteintrag> zeiteinträge)
		{
			zeiteinträge.Remove(this);
		}

		[Icon(Symbol.Add)]
		public async static Task<List<Zeiteintrag>> Neu(DateTime datum, string beschreibung,
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

				var slidingDatum = datum;
				var tokens = new[] { new Datum { Tag = datum } }.Concat(beschreibung.Replace(".", " . ").ToUpper().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).SelectMany(t =>
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
						slidingDatum = slidingDatum.AddDays(-t.Length);
						return new object[] { new Trennzeichen(), new Datum { Tag = slidingDatum } };
					}
					else if (t.All(c => c == '+'))
					{
						slidingDatum = slidingDatum.AddDays(t.Length);
						return new object[] { new Trennzeichen(), new Datum { Tag = slidingDatum } };
					}
					else if (t == ".")
					{
						return new object[] { new Trennzeichen(), new Datum { Tag = slidingDatum } };
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
				})).ToList();

				var gruppierteTokens = tokens.Aggregate(new Stack<Stack<object>>(), (accumulator, current) =>
				{
					if (current is Trennzeichen)
					{
						accumulator.Push(new Stack<object>());
					}
					else
					{
						if (accumulator.Any()) accumulator.Peek().Push(current);
						else accumulator.Push(new Stack<object>(new[] { current }));
					}
					return accumulator;
				});

				return (from gruppe in gruppierteTokens
						where !(gruppe.Count == 1 && gruppe.Peek() is Datum)
						let umgedrehteGruppe = gruppe.Reverse()
						select new Zeiteintrag
						{
							Beschreibung = CapitalizeFirstLetter(string.Join(" ", Separate(umgedrehteGruppe, (previous, next) =>
							{
								if (previous is Datum && next is Projekt) return new[] { "bezüglich" };
								else if (previous is Kunde && next is Projekt) return new[] { "bezüglich" };
								else if (previous is Tätigkeit && next is Projekt) return new[] { "bezüglich" };

								else if (previous is Datum && next is Kunde) return new[] { "mit" };
								else if (previous is Projekt && next is Kunde) return new[] { "mit" };
								else if (previous is Tätigkeit && next is Kunde) return new[] { "mit" };

								else if (previous is Projekt && next is Projekt) return new[] { "und" };
								else if (previous is Kunde && next is Kunde) return new[] { "und" };
								else if (previous is Tätigkeit && next is Tätigkeit) return new[] { "und" };

								else return new object[0];
							}))) + ".",
							Tokens = umgedrehteGruppe.ToList()
						}).ToList();
			}
		}

		private static IEnumerable<T> Separate<T>(IEnumerable<T> sequence, Func<T, T, IEnumerable<T>> separatorFunc)
		{
			T previous = default(T);
			foreach (var item in sequence.Select((e, i) => new { element = e, index = i }))
			{
				if (item.index != 0)
				{
					foreach (var separatorItem in separatorFunc(previous, item.element))
					{
						yield return separatorItem;
					}
				}
				previous = item.element;
				yield return item.element;
			}
		}

		private static string CapitalizeFirstLetter(string text)
		{
			return string.Concat(Enumerable.Concat(new[] { char.ToUpper(text.First()) }, text.Skip(1)));
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
	public class Trennzeichen
	{
		public override string ToString() => "Trennzeichen";
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

