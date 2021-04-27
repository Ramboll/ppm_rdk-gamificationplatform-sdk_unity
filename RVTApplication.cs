using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using RebarInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Utilities;

namespace RebarAnnotationTool
{
    public class RvtApplication : IExternalApplication
    {
        private Autodesk.Revit.UI.RibbonPanel _ribbonPanel;
        private UIApplication currentUIApp;
        private Autodesk.Windows.RibbonPanel rebarPanelInModifyPanel;
        private UIControlledApplication controlledApp;

        public ExternalEvent TopRebarEvent { get; set; }
        public ExternalEvent BottomRebarEvent { get; set; }
        public ExternalEvent FlipRebarEvent { get; set; }
        public ExternalEvent UpdateRebarEvent { get; set; }
        public Autodesk.Windows.RibbonButton topRebarButton { get; set; }
        public Autodesk.Windows.RibbonButton bottomRebarButton { get; set; }
        public Autodesk.Windows.RibbonButton flipRebarButton { get; set; }
        public Autodesk.Windows.RibbonButton updateRebarButton { get; set; }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication _application)
        {
            controlledApp = _application;

            // LoadMissingDependencies();

            AutoMerge.Load();

            CreateButtonsInModifyTab();


            _application.CreateRibbonTab(ConstantValues.TabName);

            _application.Idling += Application_Idling;

            _ribbonPanel = _application.CreateRibbonPanel(ConstantValues.TabName, ConstantValues.PanelName);


            PushButtonData topRebarCommand = CreatePushButton(ConstantValues.TopRebarButtonName, ConstantValues.TopRebarToolTipButtonName, ConstantValues.TopRebarCommand, Properties.Resources.Place_Top_Near_Rebar_Symbols);

            _ribbonPanel.AddItem(topRebarCommand);

            PushButtonData bottomRebarCommand = CreatePushButton(ConstantValues.BottomRebarButtonName, ConstantValues.BottomRebarToolTipButtonName, ConstantValues.BottomRebarCommand, Properties.Resources.Place_Bottom_Far_Rebar_Symbols);

            _ribbonPanel.AddItem(bottomRebarCommand);

            _ribbonPanel.AddSeparator();

            PushButtonData flipBtn = CreatePushButton(ConstantValues.FlipButtonName, ConstantValues.FlipToolTipButtonName, ConstantValues.FlipCommand, Properties.Resources.Flip_Rebar_End_Symbol);

            _ribbonPanel.AddItem(flipBtn);


            PushButtonData updateSymbols = CreatePushButton(ConstantValues.UpdateButtonName, ConstantValues.UpdateToolTipButtonName, ConstantValues.UpdateCommand, Properties.Resources.Update_Symbols);

            _ribbonPanel.AddItem(updateSymbols);

            _ribbonPanel.AddSeparator();

            PushButtonData settingsBtn = CreatePushButton(ConstantValues.SettingsButtonName, ConstantValues.SettingsToolTipButtonName, ConstantValues.SettingsCommand, Properties.Resources.Settings);

            _ribbonPanel.AddItem(settingsBtn);

            PushButtonData infoBtn = CreatePushButton(ConstantValues.InfoButtonName, ConstantValues.InfoToolTipButtonName, ConstantValues.InfoCommand, Properties.Resources.Info_Help);

            _ribbonPanel.AddItem(infoBtn);

            return Result.Succeeded;
        }



        private void CreateButtonsInModifyTab()
        {
            List<RibbonTab> lstRbnTabCollection = ComponentManager.Ribbon.Tabs.ToList();

            RibbonTab modifyRbnTab = lstRbnTabCollection.Where(x => x.AutomationName.ToLower() == "modify").Select(x => x).FirstOrDefault();

            if (modifyRbnTab != null)
            {
                rebarPanelInModifyPanel = new Autodesk.Windows.RibbonPanel();
                rebarPanelInModifyPanel.FloatingOrientation
                         = System.Windows.Controls.Orientation.Vertical;

                Autodesk.Windows.RibbonPanelSource source
            = new Autodesk.Windows.RibbonPanelSource();

                source.Name = ConstantValues.PanelName;
                source.Id = ConstantValues.PanelName;
                source.Title = ConstantValues.PanelName;

                rebarPanelInModifyPanel.Source = source;
                rebarPanelInModifyPanel.FloatingOrientation
                  = System.Windows.Controls.Orientation.Vertical;


                Autodesk.Windows.RibbonRowPanel rowPanel
                  = new Autodesk.Windows.RibbonRowPanel();


                topRebarButton = CreateRebarButton(ConstantValues.TopRebarModifyButtonName, ConstantValues.TopRebarToolTipButtonName, Properties.Resources.Place_Top_Near_Rebar_Icon.ToBitmap());

                bottomRebarButton = CreateRebarButton(ConstantValues.BottomRebarModifyButtonName, ConstantValues.BottomRebarToolTipButtonName, Properties.Resources.Place_Bottom_Far_Rebar_Icon.ToBitmap());

                flipRebarButton = CreateRebarButton(ConstantValues.FlipModifyButtonName, ConstantValues.FlipToolTipButtonName, Properties.Resources.Flip_Rebar_End_Icon.ToBitmap());



                updateRebarButton = CreateRebarButton(ConstantValues.UpdateModifyButtonName, ConstantValues.UpdateToolTipButtonName, Properties.Resources.Update.ToBitmap());

                bottomRebarButton.IsVisible = false;
                topRebarButton.IsVisible = false;
                flipRebarButton.IsVisible = false;
                updateRebarButton.IsVisible = false;


                rowPanel.Items.Add(topRebarButton);
                rowPanel.Items.Add(flipRebarButton);

                rowPanel.Items.Add(new Autodesk.Windows.RibbonRowBreak());

                rowPanel.Items.Add(bottomRebarButton);
                rowPanel.Items.Add(updateRebarButton);

                rebarPanelInModifyPanel.Source.Items.Add(rowPanel);

                modifyRbnTab.Panels.Add(rebarPanelInModifyPanel);
            }

            IExternalEventHandler topRebarCommandHandler = new TopRebarCommandHandler();
            TopRebarEvent = ExternalEvent.Create(topRebarCommandHandler);

            IExternalEventHandler bottomRebarCommandHandler = new BottomRebarCommandHandler();
            BottomRebarEvent = ExternalEvent.Create(bottomRebarCommandHandler);

            IExternalEventHandler flipRebarCommandHandler = new FlipRebarCommandHandler();
            FlipRebarEvent = ExternalEvent.Create(flipRebarCommandHandler);

            IExternalEventHandler updateRebarCommandHandler = new UpdateRebarCommandHandler();
            UpdateRebarEvent = ExternalEvent.Create(updateRebarCommandHandler);

            ComponentManager.UIElementActivated += new EventHandler<UIElementActivatedEventArgs>(ComponentManager_UIElementActivated);

        }


        public void LoadMissingDependencies()
        {
            var dependecies = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)).GetFiles("*.dll");


            AppDomain.CurrentDomain.AssemblyResolve += (send, args) =>
            {
                if (Path.GetExtension((args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name)).Equals(".resources"))
                {
                    return null;
                }

                var dll = dependecies.FirstOrDefault(fi => Path.GetFileNameWithoutExtension(fi.Name) == (args.Name.Contains(',') ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name).Replace("Application", ""));

                if (dll == null)
                {
                    return null;
                }

                if (File.Exists(dll.FullName))
                {
                    return System.Reflection.Assembly.LoadFrom(dll.FullName);
                }
                else
                {
                    return null;
                }
            };
        }

        private void RaiseTopRebarEventAction()
        {
            Thread thread = new Thread(() =>
            {
                TopRebarEvent.Raise();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void RaiseBottomRebarEventAction()
        {
            Thread thread = new Thread(() =>
            {
                BottomRebarEvent.Raise();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void RaiseFlipRebarEventAction()
        {
            Thread thread = new Thread(() =>
            {
                FlipRebarEvent.Raise();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void RaiseUpdateRebarEventAction()
        {
            Thread thread = new Thread(() =>
            {
                UpdateRebarEvent.Raise();
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private Autodesk.Windows.RibbonButton CreateRebarButton(string buttonName, string toolTipName, Bitmap bMap)
        {
            Autodesk.Windows.RibbonButton button = new Autodesk.Windows.RibbonButton();

            button.Name = buttonName;
            button.Image = BitmapToImageConverter(bMap);
            button.LargeImage = BitmapToImageConverter(bMap);
            button.Id = buttonName;
            button.AllowInStatusBar = true;
            button.AllowInToolBar = true;
            button.IsEnabled = true;
            button.IsToolTipEnabled = true;
            button.IsVisible = true;
            button.ShowImage = true;
            button.ShowText = true;
            button.ShowToolTipOnDisabled = true;
            button.Text = buttonName;
            button.ToolTip = toolTipName;
            button.MinHeight = 25;
            button.MinWidth = 25;
            button.Size = RibbonItemSize.Large;
            button.ResizeStyle = RibbonItemResizeStyles.HideText;
            button.IsCheckable = true;
            button.Orientation = System.Windows
              .Controls.Orientation.Horizontal;
            button.KeyTip = buttonName;
            return button;
        }

        private void ComponentManager_UIElementActivated(object sender, UIElementActivatedEventArgs e)
        {
            if (e.Item == null || string.IsNullOrEmpty(e.Item.Name))
            {
                ToEnableRebarPanel(false);

                return;
            }


            if (e.Item.Name == ConstantValues.TopRebarModifyButtonName)
            {
                Task.Run(new Action(RaiseTopRebarEventAction));
            }
            else if (e.Item.Name == ConstantValues.BottomRebarModifyButtonName)
            {
                Task.Run(new Action(RaiseBottomRebarEventAction));
            }
            else if (e.Item.Name == ConstantValues.FlipModifyButtonName)
            {
                Task.Run(new Action(RaiseFlipRebarEventAction));
            }
            else if (e.Item.Name == ConstantValues.UpdateModifyButtonName)
            {
                Task.Run(new Action(RaiseUpdateRebarEventAction));
            }
        }

        private PushButtonData CreatePushButton(string buttonName, string toolTipName, string commandName, Bitmap bMap)
        {
            PushButtonData linePatternBtn = new PushButtonData(buttonName, buttonName, Assembly.GetExecutingAssembly().Location, commandName);

            linePatternBtn.ToolTip = toolTipName;

            linePatternBtn.LargeImage = BitmapToImageConverter(bMap);

            linePatternBtn.Image = BitmapToImageConverter(bMap);

            return linePatternBtn;
        }

        public ImageSource BitmapToImageConverter(Bitmap bitMap)
        {
            ImageSource bmapSource = Imaging.CreateBitmapSourceFromHBitmap(bitMap.GetHbitmap(), IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(32, 32));

            return bmapSource;
        }

        private BitmapSource GetEmbeddedImage(string name)
        {
            try
            {
                Assembly a = Assembly.GetExecutingAssembly();
                Stream s = a.GetManifestResourceStream(name);
                return BitmapFrame.Create(s);
            }
            catch
            {
                return null;
            }
        }

        private void Application_Idling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            currentUIApp = (sender as UIApplication);

            ModifyTabWithAnotatePanelEnableDisabele();
        }

        private void ModifyTabWithAnotatePanelEnableDisabele()
        {
            try
            {
                if (rebarPanelInModifyPanel != null && currentUIApp.ActiveUIDocument != null && !currentUIApp.ActiveUIDocument.Document.IsFamilyDocument && currentUIApp.ActiveUIDocument.Selection.GetElementIds().Any())
                {
                    List<ElementId> lstElemId = currentUIApp.ActiveUIDocument.Selection.GetElementIds().ToList();

                    Document currentDoc = currentUIApp.ActiveUIDocument.Document;



                    if (lstElemId.Any())
                    {
                        bool isRebarElem = lstElemId.Select(x => currentDoc.GetElement(x)).ToList().Any(x => x is Rebar || x is RebarInSystem);

                        if (isRebarElem)
                        {
                            ToEnableRebarPanel(true);
                        }
                        else
                        {
                            ToEnableRebarPanel(false);
                        }
                    }
                }
                else if (rebarPanelInModifyPanel.IsVisible)
                {
                    ToEnableRebarPanel(false);
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void ToEnableRebarPanel(bool isEnable)
        {
            bottomRebarButton.IsVisible = isEnable;
            topRebarButton.IsVisible = isEnable;
            flipRebarButton.IsVisible = isEnable;
            updateRebarButton.IsVisible = isEnable;

            rebarPanelInModifyPanel.IsVisible = isEnable;
        }
    }

    public class TopRebarCommandHandler : IExternalEventHandler
    {
        public void Execute(UIApplication currentUIApp)
        {
            TopRebarCommand toprebarCommand = new TopRebarCommand();

            string empty = string.Empty;

            toprebarCommand.uiApplication = currentUIApp;

            toprebarCommand.Execute(null, ref empty, null);
        }

        public string GetName()
        {
            string name = string.Empty;

            return name;
        }
    }

    public class BottomRebarCommandHandler : IExternalEventHandler
    {
        public void Execute(UIApplication currentUIApp)
        {
            BottomRebarCommand bottomRebarCommand = new BottomRebarCommand();

            string empty = string.Empty;

            bottomRebarCommand.uiApplication = currentUIApp;

            bottomRebarCommand.Execute(null, ref empty, null);
        }

        public string GetName()
        {
            string name = string.Empty;

            return name;
        }
    }

    public class FlipRebarCommandHandler : IExternalEventHandler
    {
        public void Execute(UIApplication currentUIApp)
        {
            FlipCommand flipRebarCommand = new FlipCommand();

            string empty = string.Empty;

            flipRebarCommand.uiApplication = currentUIApp;

            flipRebarCommand.Execute(null, ref empty, null);
        }

        public string GetName()
        {
            string name = string.Empty;

            return name;
        }
    }

    public class UpdateRebarCommandHandler : IExternalEventHandler
    {
        public void Execute(UIApplication currentUIApp)
        {
            UpdateCommand updateRebarCommand = new UpdateCommand();

            string empty = string.Empty;

            updateRebarCommand.uiApplication = currentUIApp;

            updateRebarCommand.Execute(null, ref empty, null);
        }

        public string GetName()
        {
            string name = string.Empty;

            return name;
        }
    }
}
