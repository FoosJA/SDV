using Monitel.Mal;
using Monitel.Mal.Context.CIM16;
using SDV.API;
using SDV.Foundation;
using SDV.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using measValAPI = SDV.Foundation.MeasurementValueAPI;
using MeasurementValue = Monitel.Mal.Context.CIM16.MeasurementValue;


namespace SDV
{
	class AppViewModel : AppViewModelBase
	{
		#region свойства
		APIrequests.TokenResponse TokenRead { get; set; }

		public List<HalfHourMeas> SelectedHList = new List<HalfHourMeas>();

		private HalfHourMeas _selectedH;
		public HalfHourMeas SelectedH
		{
			get { return _selectedH; }
			set { _selectedH = value; RaisePropertyChanged(); }
		}
		private ObservableCollection<MeasValue> _measValueH;
		public ObservableCollection<MeasValue> MeasValueH
		{
			get { return _measValueH; }
			set { _measValueH = value; RaisePropertyChanged(); }
		}
		private ObservableCollection<HalfHourMeas> _oiHList = new ObservableCollection<HalfHourMeas>();
		public ObservableCollection<HalfHourMeas> OiHList
		{
			get { return _oiHList; }
			set { _oiHList = value; RaisePropertyChanged(); }
		}

		public List<SdvMeas> SelectedSdvList = new List<SdvMeas>();

		private SdvMeas _selectedSdv;
		public SdvMeas SelectedSdv
		{
			get { return _selectedSdv; }
			set
			{
				_selectedSdv = value;
				MeasValueH = SelectedSdv.W.MeasValueList;
				RaisePropertyChanged();
			}
		}
		private ObservableCollection<SdvMeas> _sdvList = new ObservableCollection<SdvMeas>();
		public ObservableCollection<SdvMeas> SdvList
		{
			get { return _sdvList; }
			set { _sdvList = value; RaisePropertyChanged(); }
		}
		private DateTime _startTime = DateTime.Now.Date.AddDays(-1);
		public string StartTimeStr
		{
			get { return _startTime.ToString("f"); }
			set
			{
				try
				{
					_startTime = Convert.ToDateTime(value);
					RaisePropertyChanged();
				}
				catch (Exception ex)
				{
					Log("Некорректная дата " + ex.Message);
				}

			}
		}
		private DateTime _endTime = DateTime.Now.Date;
		public string EndTimeStr
		{
			get { return _endTime.ToString("f"); }
			set
			{
				try
				{
					_endTime = Convert.ToDateTime(value);
					RaisePropertyChanged();
				}
				catch (Exception ex)
				{
					Log("Некорректная дата " + ex.Message);
				}

			}
		}

		/// <summary>
		/// Все формулы в СК-2007
		/// </summary>
		public ObservableCollection<Formulas> CalcValues { get; set; }
		/// <summary>
		/// Все опранды формул СК-2007
		/// </summary>
		public ObservableCollection<OperandFrm> OperandCollect { get; set; }
		/// <summary>
		/// СДВ принимаемые в СК-2007
		/// </summary>
		public ObservableCollection<OIck07> TransmitCollect { get; set; }
		/// <summary>
		/// Все агрегируемые в СК-2007
		/// </summary>
		public ObservableCollection<IntegParam> AgrCollect { get; set; }
		public ObservableCollection<DrSource> DrCollect { get; set; }

		private ModelImage mImage;
		private string BaseUrl;
		private StoreDB dB = new StoreDB();
		private CancellationTokenSource _tokenSource = new CancellationTokenSource();
		private Function FuncAIP;
		private TabItems _selectedItem = TabItems.H;

		public TabItems SelectItem
		{
			get { return _selectedItem; }
			set { _selectedItem = value; RaisePropertyChanged(); }
		}
		public enum TabItems
		{
			H,
			SDV
		}
		#endregion


		#region Команды
		/// <summary>
		/// Подключение и чтение БД
		/// </summary>
		public ICommand ConnectCommand { get { return new RelayCommand(ConnectExecute); } }
		public void ConnectExecute()
		{
			//LoadingData = true;
			ConnectWindow connectWindow = new ConnectWindow() { Owner = App.Current.MainWindow };
			try
			{
				connectWindow.ShowDialog();
				BaseUrl = connectWindow.BaseUrl;
				mImage = connectWindow.mImage;
				dB = connectWindow.DataBase;
			}
			catch (Exception ex)
			{
				Log($"Ошибка: {ex.Message}");
			}

			if (mImage != null)
			{
				OiHList.Clear();
				FuncAIP = new Function(mImage, CreateRepVal);
				MetaClass hisClass = mImage.MetaData.Classes["HISPartition"];
				IEnumerable<HISPartition> hisCollect = mImage.GetObjects(hisClass).Cast<HISPartition>();
				var hisH = hisCollect.First(x => x.Uid == new Guid("1000007B-0000-0000-C000-0000006D746C"));//Аналоговые 1 ч и 30 минут
				var hisW = hisCollect.First(x => x.Uid == new Guid("1000007D-0000-0000-C000-0000006D746C"));

				MetaClass avClass = mImage.MetaData.Classes["AnalogValue"];
				IEnumerable<AnalogValue> avCollect = mImage.GetObjects(avClass).Cast<AnalogValue>();

				List<OIck11> oi11List = new List<OIck11>();
				ObservableCollection<AnalogValue> sdvAvList = new ObservableCollection<AnalogValue>(avCollect.Where(x =>
				(x.HISPartition == hisH) || (x.HISPartition == hisW)));
				foreach (AnalogValue av in sdvAvList)
				{
					MemberInfo[] memberArray = av.GetType().GetMembers();
					string nameClass = memberArray[0].DeclaringType.Name;
					OIck11 oi = new OIck11
					{
						Name = av.name,
						UidMeas = av.Analog.Uid,
						UidVal = av.Uid,
						HISpartition = av.HISPartition.name,
						ValueSource = av.MeasurementValueSource.name,
						Class = nameClass,
						MeasType = av.Analog.MeasurementType.name,
						ValueType = av.MeasurementValueType.name
					};

					if (av is ReplicatedAnalogValue avRep)
					{
						oi.Id = avRep.sourceId;
					}
					else
					{
						oi.Id = av.externalId?.Replace("Calc", "").Replace("Agr", "").Replace("RB", "");
					}

					if (oi.Id is null || oi.Id == String.Empty)
					{
						//TODO: 
						//Log($"Для ОИ не найден id: {oi.Name} uid={oi.UidVal}");
					}
					else
					{
						oi11List.Add(oi);
					}
				}

				var sdvCollect = from h in oi11List.Where(x => x.HISpartition == hisH.name)
								 join w in oi11List.Where(x => x.HISpartition == hisW.name) on new { h.UidMeas, V = h.Id.Remove(0, 1) } equals new { w.UidMeas, V = w.Id.Remove(0, 1) } into gj
								 from subnet in gj.DefaultIfEmpty()
								 select new SdvMeas { H = h, W = subnet ?? null };
				SdvList = new ObservableCollection<SdvMeas>(sdvCollect.Where(x => x.W != null));

				Log($"Чтение ИМ выполнено!");
				ObservableCollection<OIck07> Oi07List = new ObservableCollection<OIck07>();
				if (dB != null)
				{
					try
					{
						Oi07List = dB.GetAllOI();
						CalcValues = dB.GetCalcValue();
						OperandCollect = dB.GetOperands();
						TransmitCollect = dB.GetTransmitOi();
						AgrCollect = dB.GetIntegParam();
						DrCollect = dB.GetDrSource();
						Log($"Чтение БД СК-07 выполнено!");
					}
					catch (Exception ex)
					{
						Log("Ошибка подключения к СК-07: " + ex.Message);
					}
				}

				var twoMeasCollect = from oi11 in sdvCollect.Where(x => x.W == null)
									 join oi7 in Oi07List on oi11.H.Id equals oi7.Id
									 select new HalfHourMeas { OIck07 = oi7, OIck11 = oi11.H };

				OiHList = new ObservableCollection<HalfHourMeas>(twoMeasCollect);
				Log($"Готово!");
			}

		}
		/// <summary>
		/// Создание часового параметра
		/// </summary>
		public ICommand CreateCommand { get { return new RelayCommand(CreateExecute, CanCreate); } }

		private bool CanCreate() { return SelectedH != null && SelectItem == TabItems.H; ; }
		async void CreateExecute()
		{
			var progress = new Progress<int>();
			progress.ProgressChanged += ((sender, e) => { CurrentProgress = e; });
			await CreateAsync(_tokenSource.Token, progress);
		}
		async Task CreateAsync(CancellationToken token = default, IProgress<int> progress = null)
		{
			CurrentProgress = 0;
			ProgressMax = SelectedHList.ToList().Count;
			LoadingData = true;
			Log("Обработка начата");
			foreach (HalfHourMeas h in SelectedHList.ToArray())
			{
				await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
				if (token.IsCancellationRequested)
				{
					break;
				}

				var isTransmit = TransmitCollect.FirstOrDefault(x => x.Id == h.OIck07.Id);
				var isAgregH = AgrCollect.FirstOrDefault(x => x.CategoryOI + x.IdOI == h.OIck07.Id);
				var isAgregW = AgrCollect.FirstOrDefault(x => x.CategoryOI + x.IdOI == h.OIck07.Id.Replace('H', 'W'));
				var isCalcH = CalcValues.FirstOrDefault(x => x.CatRes + x.IdRes.ToString() == h.OIck11.Id);
				var isDrW = DrCollect.FirstOrDefault(x => x.Id == h.OIck07.Id.Replace('H', 'W'));
				var isDrH = DrCollect.FirstOrDefault(x => x.Id == h.OIck07.Id);

				if (isTransmit != null || h.OIck07.CategoryH == "Внешняя система" || h.OIck07.CategoryW == "Внешняя система")
				{
					try
					{
						var newW = FuncAIP.CreateRBvalue(h.OIck11);
						OiHList.Remove(h);
						SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
						SdvList.Add(sdv);
						Log($"Создано {newW.Id} RapidBus");
					}
					catch (Exception ex) { Log($"Ошибка создания RB значения {h.OIck11.Id}: {ex.Message}"); }
				}
				else if (h.OIck07.CategoryH == "Повтор за предыдущий цикл" || h.OIck07.CategoryW == "Повтор за предыдущий цикл" ||
					h.OIck07.CategoryH == "Повтор значения в 0 часов" || h.OIck07.CategoryW == "Повтор значения в 0 часов")
				{
					OIck11 newW = FuncAIP.CreateRepeatedValue(h.OIck11);
					OiHList.Remove(h);
					SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
					SdvList.Add(sdv);
					Log($"Создано {newW.Id} Повторяемое");
				}

				else if (isDrW != null)
				{
					try
					{
						OIck11 newW;
						if (isDrW.IdSource == isDrW.Id.Replace('W', 'H'))
						{
							newW = FuncAIP.CreateAgregateValue(h.OIck11, isDrH);
						}
						else
						{ newW = FuncAIP.CreateAgregateValue(h.OIck11, isDrW); }
						OiHList.Remove(h);
						SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
						SdvList.Add(sdv);
						Log($"Создано {newW.Id} Агрегирование");
					}
					catch (Exception ex)
					{
						Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
					}
				}
				else if (isAgregW != null)
				{
					if (isAgregW.IdSource == isAgregW.IdOI &&
						isAgregW.CategorySource.Replace('H', 'W') == isAgregW.CategoryOI)
					{ //тогда делаем как H	
						if (isCalcH != null)
						{
							try
							{
								var newW = FuncAIP.CreateCalcvalue(h.OIck11, 'H', CalcValues, OperandCollect);
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Дорасчёт");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания дорасчёта для {h.OIck11.Id}: {ex.Message}");
							}
						}
						else if (isAgregH != null)
						{
							try
							{
								var newW = FuncAIP.CreateAgregateValue(h.OIck11, AgrCollect.ToList());
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Агрегирование");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
							}
						}
						else if (isDrH != null)
						{
							try
							{
								var newW = FuncAIP.CreateAgregateValue(h.OIck11, isDrH);
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Агрегирование");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
							}
						}
						else { Log($"Проверить! Ошибка создания  {h.OIck11.Id}"); }
					}
					else
					{
						try
						{
							var newW = FuncAIP.CreateAgregateValue(h.OIck11, AgrCollect.ToList());
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Агрегирование");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
						}
					}

				}
				else if (h.OIck07.CategoryW == "Дорасчет")
				{
					Formulas formulas = new Formulas();
					try
					{
						formulas = CalcValues.First(x => x.CatRes == "W" && x.IdRes.ToString() == h.OIck11.Id.Remove(0, 1));
					}
					catch { Log($"Формула для { h.OIck07.Id} не найдена"); }
					var operands = (List<OperandFrm>)OperandCollect.Where(x => x.FID == formulas.FID && x.TypeFrm == formulas.TypeFrm).ToList();
					string idOperand = operands[0].CatOperand + operands[0].IdOperand;
					if (operands.Count() == 1 && idOperand == h.OIck11.Id)
					{
						//тогда делаем как H	
						if (isCalcH != null)
						{
							try
							{
								var newW = FuncAIP.CreateCalcvalue(h.OIck11, 'H', CalcValues, OperandCollect);
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Дорасчёт");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания дорасчёта для {h.OIck11.Id}: {ex.Message}");
							}
						}
						else if (isAgregH != null)
						{
							try
							{
								var newW = FuncAIP.CreateAgregateValue(h.OIck11, AgrCollect.ToList());
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Агрегирование");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
							}
						}
						else if (isDrH != null)
						{
							try
							{
								var newW = FuncAIP.CreateAgregateValue(h.OIck11, isDrH);
								OiHList.Remove(h);
								SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
								SdvList.Add(sdv);
								Log($"Создано {newW.Id} Агрегирование");
							}
							catch (Exception ex)
							{
								Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
							}
						}
						else { Log($"Проверить! Ошибка создания  {h.OIck11.Id}"); }

					}
					else
					{
						try
						{
							//создаем значение W дорасчёт
							var newW = FuncAIP.CreateCalcvalue(h.OIck11, 'W', CalcValues, OperandCollect);
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Дорасчёт");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания дорасчёта для {h.OIck11.Id}: {ex.Message}");
						}
					}
				}
				else if (h.OIck07.CategoryW == "Без заполнения" || h.OIck07.CategoryW == "Источник ПВ" || h.OIck07.CategoryW == "Обнуление")
				{
					//тогда делаем как H	
					if (isCalcH != null)
					{
						try
						{
							var newW = FuncAIP.CreateCalcvalue(h.OIck11, 'H', CalcValues, OperandCollect);
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Дорасчёт");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания дорасчёта для {h.OIck11.Id}: {ex.Message}");
						}
					}
					else if (isAgregH != null)
					{
						try
						{
							var newW = FuncAIP.CreateAgregateValue(h.OIck11, AgrCollect.ToList());
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Агрегирование");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
						}
					}
					else if (isDrH != null)
					{
						try
						{
							var newW = FuncAIP.CreateAgregateValue(h.OIck11, isDrH);
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Агрегирование");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
						}
					}
					else if (h.OIck07.CategoryH == "Обнуление")
					{
						try
						{
							OIck11 newW = FuncAIP.CreateRepeatedValue(h.OIck11);
							OiHList.Remove(h);
							SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
							SdvList.Add(sdv);
							Log($"Создано {newW.Id} Повторяемое");
						}
						catch (Exception ex)
						{
							Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
						}
					}
					else
					{
						Log($"Проверить! Ошибка создания  {h.OIck11.Id}");
					}
				}
				else if (h.OIck07.CategoryH == "Обнуление")
				{
					try
					{
						OIck11 newW = FuncAIP.CreateRepeatedValue(h.OIck11);
						OiHList.Remove(h);
						SdvMeas sdv = new SdvMeas { H = h.OIck11, W = newW };
						SdvList.Add(sdv);
						Log($"Создано {newW.Id} Повторяемое");
					}
					catch (Exception ex)
					{
						Log($"Ошибка создания агрегированного значения {h.OIck11.Id}: {ex.Message}");
					}
				}
				else
				{
					Log($"Проверить! Ошибка создания {h.OIck11.Id}");
				}
				CurrentProgress++;
				progress?.Report(CurrentProgress);

			}
			Log("Обработка завершена");
			LoadingData = false;
		}
		/// <summary>
		/// Запрос архива из СК-2007
		/// </summary>
		public ICommand GetArhiveCommand { get { return new RelayCommand(GetArhiveExecute, CanGetArhive); } }

		private bool CanGetArhive() { return SelectedSdv != null; }

		public void GetArhiveExecute()
		{
			if(_startTime>_endTime)
			{
				Log("Некорректный интервал времени");				
			}
			else
			{
				DateTime dtStart = Convert.ToDateTime(_startTime.ToString("s"));
				DateTime dtEnd = Convert.ToDateTime(_endTime.ToString("s"));
				try
				{
					foreach(var sdv in SelectedSdvList)
					{
						dB.GetValueOI(dtStart, dtEnd, sdv);
						Log($"Архив {sdv.H.Id} запрошен"); ;
					}					
				}
				catch (Exception ex)
				{ Log("Ошибка запроса архива: " + ex.Message); }
			}
			

		}

		#endregion

		#region Стандартные команды


		public ICommand StopLoadDataCommand
		{
			get
			{
				return new RelayCommand(StopLoadExecute, CanStopLoadData);
			}
		}
		private bool CanStopLoadData()
		{
			return LoadingData;
		}
		public void StopLoad()
		{
			Log("Получена команда остановки.");
			_tokenSource.Cancel();
			_tokenSource = new CancellationTokenSource();
		}
		private void StopLoadExecute()
		{
			StopLoad();
		}
		private bool _compareModeIsActive = false;
		public bool CompareModeIsActive
		{
			get => _compareModeIsActive;
			set
			{
				_compareModeIsActive = value;
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(CompareModeIsActiveAndNotLoadingData));
			}
		}
		public bool NotLoadingData => !LoadingData;
		public bool CompareModeIsActiveAndNotLoadingData => CompareModeIsActive && NotLoadingData;
		private bool _loadingData;
		public bool LoadingData
		{
			get => _loadingData;
			private set
			{
				_loadingData = value;
				RaisePropertyChanged();
				RaisePropertyChanged(nameof(NotLoadingData));
				RaisePropertyChanged(nameof(CompareModeIsActiveAndNotLoadingData));
				CommandManager.InvalidateRequerySuggested();
			}
		}
		public ICommand ClearInfoCollect { get { return new RelayCommand(ClearInfoExecute); } }
		private void ClearInfoExecute() { InfoCollect.Clear(); }

		public ICommand CopyCommand { get { return new RelayCommand(CopyUidTempExecute); } }

		private void CopyUidTempExecute() { Clipboard.SetText(SelectedH.OIck11.UidVal.ToString()); }

		public ICommand SettingsCommand { get { return new RelayCommand(SettingsExecute, CanCorrectSettings); } }
		private bool CanCorrectSettings() { return mImage != null; }
		public bool CreateRepVal { get; set; }
		public Analog AnalogForVal { get; set; }
		public Discrete DiscreteForVal { get; set; }


		public Guid AnalogUidView { get; set; }
		public void SettingsExecute()
		{
			SettingsWindow settingstWindow = new SettingsWindow(FuncAIP.Prefix, CreateRepVal) { Owner = App.Current.MainWindow };
			settingstWindow.ShowDialog();
			if (settingstWindow.SaveChange)
			{
				if ((Analog)mImage.GetObject(settingstWindow.GuidAnalog) == null)
				{
					Log($"В ИМ не найден Аналог {settingstWindow.GuidAnalog}");
				}
				else
				{
					AnalogForVal = (Analog)mImage.GetObject(settingstWindow.GuidAnalog);
				}
				if ((Discrete)mImage.GetObject(settingstWindow.GuidDiscrete) == null)
				{
					Log($"В ИМ не найден Дискрет {settingstWindow.GuidDiscrete}");
				}
				else
				{
					DiscreteForVal = (Discrete)mImage.GetObject(settingstWindow.GuidDiscrete);
				}
				FuncAIP.Prefix = settingstWindow.NameOi;
				CreateRepVal = settingstWindow.TriggerСreateRep;
				FuncAIP.CreateRepVal = CreateRepVal;
				FuncAIP.UidAnalogForVal = settingstWindow.GuidAnalog;
				FuncAIP.UidDiscreteForVal = settingstWindow.GuidDiscrete;
				Log("Настройки сохранены");
			}
		}
		#endregion


	}


	public class HalfHourMeas
	{
		public OIck07 OIck07 { get; set; }
		public OIck11 OIck11 { get; set; }
	}
	public class SdvMeas
	{
		public OIck11 H { get; set; }
		public OIck11 W { get; set; }
	}
}


