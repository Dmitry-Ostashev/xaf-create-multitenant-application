﻿using System.Reactive.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using DevExpress.ExpressApp.ViewVariantsModule;
using Unit = System.Reactive.Unit;
using View = DevExpress.ExpressApp.View;

namespace XAF.Testing.XAF{
    public static class FrameExtensions{
        public static IObservable<Window> WhenMaximized(this IObservable<Window> source) 
            => source.SelectMany(WhenMaximized);

        private static IObservable<Window> WhenMaximized(this Window window) 
            => window.Application.GetRequiredService<IWindowMaximizer>().WhenMaximized(window);

        public static IObservable<T> SelectUntilViewClosed<T,TFrame>(this IObservable<TFrame> source, Func<TFrame, IObservable<T>> selector) where TFrame:Frame 
            => source.SelectMany(frame => selector(frame).TakeUntilViewClosed(frame));
        
        public static IObservable<TFrame> TakeUntilViewClosed<TFrame>(this IObservable<TFrame> source,Frame frame)  
            => source.TakeUntil(frame.View.WhenClosing());
        public static bool When<T>(this T frame, params Nesting[] nesting) where T : Frame 
            => nesting.Any(item => item == Nesting.Any || frame is NestedFrame && item == Nesting.Nested ||
                                   !(frame is NestedFrame) && item == Nesting.Root);

        public static bool When<T>(this T frame, params string[] viewIds) where T : Frame 
            => viewIds.Contains(frame.View?.Id);

        public static bool When<T>(this T frame, params ViewType[] viewTypes) where T : Frame 
            => viewTypes.Any(viewType =>viewType==ViewType.Any|| frame.View is CompositeView compositeView && compositeView.Is(viewType));

        public static bool When<T>(this T frame, params Type[] types) where T : Frame 
            => types.Any(item => frame.View is ObjectView objectView && objectView.Is(objectType:item));
        

        public static IObservable<Frame> ListViewProcessSelectedItem<T>(this Frame frame, Func<T> selectedObject){
            var action = frame.GetController<ListViewProcessCurrentObjectController>().ProcessCurrentObjectAction;
            return action.Trigger(action.WhenExecuted()
                .SelectMany(e => frame.Application.WhenFrame(e.ShowViewParameters.CreatedView.ObjectTypeInfo.Type,ViewType.DetailView))
                .Take(1).Select(frame1 => frame1),selectedObject().YieldItem().Cast<object>().ToArray())
                .Select(frame1 => frame1);
        }
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params ViewType[] viewTypes) where T : Frame
            => source.Where(frame => frame.When(viewTypes));
        
        public static IObservable<T> ToController<T>(this IObservable<Frame> source) where T : Controller 
            => source.SelectMany(window => window.Controllers.Cast<Controller>()).OfType<T>();

        public static IObservable<Frame> CloseWindow(this IObservable<Frame> source, Frame previous) 
            => source.If(frame => frame is not Window,frame => Observable.Throw<Frame>(new InvalidCastException($"{frame.View}")),frame => frame.Observe())
                .Cast<Window>().DelayOnContext()
                .SelectMany(frame => {
                    var dialogController = frame.GetController<DialogController>();
                    return dialogController switch {
                        { AcceptAction.Active.ResultValue: true } => dialogController.AcceptAction.Trigger().Take(1).To<Frame>().Concat(previous.Observe()),
                        { CloseAction.Active.ResultValue: true } => dialogController.CloseAction.Trigger().Take(1).To<Frame>().Concat(previous.Observe()),
                        null => frame.IsMain ? frame.Application.NavigateBack() : frame.Actions("Close").Cast<SimpleAction>().Where(a => a.Controller.Name.StartsWith("DevExpress")).ToNowObservable()
                            .If(action => action.Active,action => action.Trigger().To(previous),action => frame.Observe().Do(window => window.Close()).To(previous)),
                        _ => throw new NotImplementedException()
                    };
                })
                .DelayOnContext();

        public static IObservable<Frame> WhenDialogAccept(this Frame frame, Frame parentFrame)
            => frame.Defer(_ => {
                var dialogController = frame.GetController<DialogController>();
                return dialogController != null ? dialogController.AcceptAction.Trigger(parentFrame.Observe()).Select(frame2 => frame2) : frame.Observe();
            });
        public static IObservable<Frame> WhenAggregatedSave(this Frame frame,Frame parentFrame) 
            => frame.Observe()
                .If(_ => frame is NestedFrame{ ViewItem: PropertyEditor editor } && editor.MemberInfo.IsAggregated,
                    _ => parentFrame.Actions("Save").Cast<SimpleAction>().ToNowObservable().WhenAvailable()
                        .SelectMany(a => a.Trigger().To(frame)),
                    _ => frame.Observe());
        
        public static IObservable<T> WhenAcceptTriggered<T>(this IObservable<DialogController> source,IObservable<T> afterExecuted,params object[] selection) 
            => source.SelectMany(controller => controller.AcceptAction.Trigger(afterExecuted,selection).Take(1));
        
        public static IObservable<DashboardViewItem> DashboardViewItem(this IObservable<Frame> source, Func<DashboardViewItem, bool> itemSelector) 
            => source.SelectMany(frame => frame.DashboardViewItem(itemSelector));

        public static IObservable<(Frame frame, Frame parent)> MasterFrame(this IObservable<(Frame frame, Frame parent)> source, Func<DashboardViewItem, bool> itemSelector = null)
            => source.Select(t => (t.frame.MasterFrame(itemSelector),t.parent));
        public static Frame MasterFrame(this Frame frame, Func<DashboardViewItem, bool> itemSelector = null)
            => frame.View is DashboardView? frame.DashboardViewItem( itemSelector).ToFrame().First():frame;
        
        public static IEnumerable<DashboardViewItem> DashboardViewItem(this Frame frame,Func<DashboardViewItem, bool> itemSelector=null) 
            => frame.DashboardViewItems(ViewType.DetailView).Where(item => item.MasterViewItem(itemSelector))
                .SwitchIfEmpty(frame.DashboardViewItems(ViewType.ListView).Where(item => item.MasterViewItem(itemSelector)));
        
        public static bool MasterViewItem(this DashboardViewItem item,Func<DashboardViewItem, bool> masterItem=null) 
            => masterItem?.Invoke(item)??item.Model.ActionsToolbarVisibility != ActionsToolbarVisibility.Hide;
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params Nesting[] nesting) where T:Frame 
            => source.Where(frame => frame.When(nesting));
        
        public static IObservable<T> WhenFrame<T>(this IObservable<T> source, params string[] viewIds) where T:Frame 
            => source.Where(frame => frame.When(viewIds));
        
        public static IObservable<TFrame> WhenTemplateChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.WhenEvent(nameof(Frame.TemplateChanged)).Select(pattern => pattern).To(item)
                .TakeUntil(item.WhenDisposedFrame());

        public static IObservable<TFrame> When<TFrame>(this IObservable<TFrame> source, TemplateContext templateContext) where TFrame : Frame 
            => source.Where(window => window.Context == templateContext);
        
        public static IObservable<T> TemplateChanged<T>(this IObservable<T> source) where T : Frame 
            => source.SelectMany(item => item.Template != null ? item.Observe() : item.WhenTemplateChanged().Select(_ => item));
        public static IObservable<T> WhenFrame<T>(this T frame,ViewType viewType, params Type[] types) where T : Frame 
            => frame.View != null ? frame.When(viewType) && frame.When(types) ? frame.Observe() : Observable.Empty<T>()
                : frame.WhenViewChanged().Where(t => t.When(viewType) && t.When(types)).To(frame);
        public static IObservable<T> WhenFrame<T>(this T frame, params string[] viewIds) where T : Frame 
            => frame.WhenViewChanged().Where(frame1 => viewIds.Contains(frame1.View.Id));
        public static IObservable<TFrame> WhenViewChanged<TFrame>(this IObservable<TFrame> source) where TFrame : Frame
            => source.SelectMany(frame => frame.WhenViewChanged());
        
        public static IObservable<TFrame > WhenViewChanged<TFrame>(this TFrame item) where TFrame : Frame 
            => item.WhenEvent<ViewChangedEventArgs>(nameof(Frame.ViewChanged))
                .TakeUntil(item.WhenDisposedFrame()).Select(_ => item);

        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,params Type[] objectTypes) 
            => frame.View.ToCompositeView().DashboardViewItems(objectTypes);

        public static IEnumerable<DashboardViewItem> DashboardViewItems(this Frame frame,ViewType viewType,params Type[] objectTypes) 
            => frame.DashboardViewItems(objectTypes).When(viewType);

        public static IEnumerable<DashboardViewItem> When(this IEnumerable<DashboardViewItem> source, params ViewType[] viewTypes) 
            => source.Where(item => viewTypes.All(viewType => item.InnerView.Is(viewType)));

        public static IEnumerable<TViewType> DashboardViewItems<TViewType>(this Frame frame,params Type[] objectTypes) where TViewType:View
            => frame.DashboardViewItems(objectTypes).ToFrame().Select(nestedFrame => nestedFrame.View as TViewType).WhereNotDefault();
        
        public static IObservable<ListPropertyEditor> NestedListViews(this Frame frame, params Type[] objectTypes ) 
            => frame.View.ToDetailView().NestedListViews(objectTypes);
        
        public static DetailView ToDetailView(this View view) => (DetailView)view;

        public static IObservable<Frame> DashboardDetailViewFrame(this Frame frame) 
            => frame.DashboardViewItems(ViewType.DetailView).Where(item => !item.MasterViewItem()).ToNowObservable()
                .ToFrame();

        public static IObservable<DetailView> ToDetailView<T>(this IObservable<T> source) where T : Frame
            => source.Select(frame => frame.View.ToDetailView());
        public static IObservable<Unit> WhenDisposedFrame<TFrame>(this TFrame source) where TFrame : Frame
            => source.WhenEvent(nameof(Frame.Disposed)).ToUnit();
        
        
        public static IEnumerable<ActionBase> Actions(this Frame frame,params string[] actionsIds) 
            => frame.Actions<ActionBase>(actionsIds);
        
        public static IEnumerable<T> Actions<T>(this Frame frame,params string[] actionsIds) where T : ActionBase 
            => frame.Controllers.Cast<Controller>().SelectMany(controller => controller.Actions).OfType<T>()
                .Where(actionBase => !actionsIds.Any()|| actionsIds.Any(s => s==actionBase.Id));

        public static Window ToActive(this Window window) 
            => window.Application.GetRequiredService<IActiveWindowResolver>().GetWindow(window);

        public static IObservable<Frame> ChangeViewVariant(this IObservable<Frame> source,string id) 
            => source.ToController<ChangeVariantController>()
                .SelectMany(controller => {
                    var choiceActionItem = controller.ChangeVariantAction.Items.First(item => item.Id == id);
                    var variantInfo = ((VariantInfo)choiceActionItem.Data);
                    return variantInfo.ViewID != controller.Frame.View.Id ? controller.ChangeVariantAction.Trigger(
                            controller.Application.WhenFrame(variantInfo.ViewID), () => choiceActionItem) : controller.Frame.Observe();
                });

        public static IObservable<object> WhenObjects(this IObservable<Frame> source,int count=0) 
            => source.SelectMany(frame => frame.WhenObjects(count)).Select(t => t);

        public static IObservable<object> WhenObjects(this Frame frame,int count=0) 
            => frame.Application.GetRequiredService<IFrameObjectObserver>().WhenObjects(frame,count);

        public static NestedFrame ToNestedFrame(this Frame frame) => (NestedFrame)frame;
        public static IObservable<Frame> SelectDashboardListViewObject(this IObservable<Frame> source, Func<DashboardViewItem, bool> itemSelector=null) 
            => source.SelectDashboardColumnViewObject(itemSelector??(item =>item.MasterViewItem()) )
                .SwitchIfEmpty(Observable.Defer(() => source.SelectMany(window => window.DashboardViewItems(ViewType.ListView).ToNowObservable()
                    .Where(itemSelector??(_ =>true) ).Select(item => item.InnerView.ToListView())
                    .SelectMany(listView => listView.SelectObject()).To(window))));
        
        private static IObservable<Frame> SelectDashboardColumnViewObject(this IObservable<Frame> source,Func<DashboardViewItem,bool> itemSelector=null) 
            => source.SelectMany(frame => frame.SelectDashboardColumnViewObject(itemSelector));

        public static IObservable<Frame> SelectDashboardColumnViewObject(this Frame frame,Func<DashboardViewItem, bool> itemSelector) 
            => frame.DashboardViewItems(ViewType.DetailView).Where(itemSelector ?? (_ => true)).ToNowObservable()
                .SelectMany(item => frame.Application.GetRequiredService<IDashboardColumnViewObjectSelector>()
                    .SelectDashboardColumnViewObject(item).To(frame));

        public static IObservable<Frame> CreateNewObject(this Frame frame,bool inLine=false) 
            => !inLine ? frame.CreateNewObjectController() : frame.CreateNewObjectEditor();

        private static IObservable<Frame> CreateNewObjectEditor(this Frame frame) 
            => Observable.Defer(() => frame.View.WhenControlsCreated()
                .StartWith(frame.View.ToListView().Editor.Control).WhenNotDefault()
                .SelectMany(_ => frame.View.ToListView().WhenObjects(1)
                    .Do(frame.AddNewRowAndCloneMembers))
                .To(frame));

        private static void AddNewRowAndCloneMembers(this Frame frame, object existingObject) 
            => frame.Application.GetRequiredService<INewRowAdder>().AddNewRowAndCloneMembers(frame,existingObject);
        
        public static IObservable<Frame> CreateNewObjectController(this Frame frame) 
            => frame.WhenObjects(1).Take(1).SelectMany(selectedObject => frame.ColumnViewCreateNewObject()
                    .SwitchIfEmpty(frame.ListViewCreateNewObject())
                    .SelectMany(newObjectFrame => newObjectFrame.View.ToCompositeView()
                        .CloneExistingObjectMembers(false, selectedObject)
                        .Select(_ => default(Frame)).IgnoreElements().Concat(newObjectFrame.YieldItem())));

        internal static IObservable<Frame> ColumnViewCreateNewObject(this Frame frame) 
            => frame.View.Observe().OfType<DetailView>()
                .SelectMany(view => view.WhenGridControl().Take(1).To(frame).CreateNewObject());

        public static IObservable<object> WhenGridControl(this DetailView detailView) 
            => detailView.ObjectSpace.GetRequiredService<IUserControlProvider>().WhenGridControl(detailView);

        public static IObservable<Frame> CreateNewObject(this IObservable<Frame> source)
            => source.ToController<NewObjectViewController>().Select(controller => controller.NewObjectAction).Take(1)
                .SelectMany(action => action.Trigger(action.Application
                    .WhenFrame(action.Controller.Frame.View.ObjectTypeInfo.Type, ViewType.DetailView)
                    .Merge(action.Controller.Frame.View.AsListView().Observe().WhenNotDefault().Select(view => view.EditView).WhenNotDefault()
                        .SelectMany(view => view.WhenCurrentObjectChanged().Where(detailView => detailView.IsNewObject()))
                        .To(action.Controller.Frame))
                    .Take(1)));

        public static IObservable<Frame> ListViewCreateNewObject(this Frame frame) 
            => (frame.View is not DashboardView ? frame.Observe()
                : frame.DashboardViewItems(ViewType.ListView).Where(item => item.Model.ActionsToolbarVisibility != ActionsToolbarVisibility.Hide)
                    .ToFrame().ToNowObservable())
                .CreateNewObject();
        
        

        public static IObservable<(Frame frame, Frame detailViewFrame)> ProcessSelectedObject(this Frame frame) 
            => frame.Application.GetRequiredService<ISelectedObjectProcessor>().ProcessSelectedObject(frame);

        public static IObservable<Frame> AssertDashboardViewGridControlDetailViewObjects(this Frame frame, params string[] relationNames) 
            => frame.Application.GetRequiredService<IDashboardViewGridControlDetailViewObjectsAsserter>()
                .AssertDashboardViewGridControlDetailViewObjects(frame,relationNames);

        public static DetailView DashboardChildDetailView(this NestedFrame listViewFrame) 
            => ((DashboardView)listViewFrame.ViewItem.View).Views<DetailView>().First(detailView => detailView!=listViewFrame.View);

        public static IObservable<Frame> ProcessListViewSelectedItem(this Frame frame) 
            => frame.View is not DashboardView ? frame.WhenObjects(1).Take(1).SelectMany(o => frame.ListViewProcessSelectedItem(() => o))
                : frame.DashboardViewItems(ViewType.ListView).ToNowObservable().ToFrame()
                    .SelectMany(frame1 => frame1.View.ToListView().WhenObjectViewObjects(1).Take(1)
                        .SelectMany(o => frame1.ListViewProcessSelectedItem(() => o)));

        
        public static object ParentObject(this Frame frame) => frame.ParentObject<object>() ;

        public static T ParentObject<T>(this Frame frame) where T : class
            => frame.ToNestedFrame().ViewItem.CurrentObject as T;
    }
}