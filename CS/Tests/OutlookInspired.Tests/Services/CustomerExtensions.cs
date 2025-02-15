﻿using System.Reactive;
using System.Reactive.Linq;
using DevExpress.ExpressApp;
using OutlookInspired.Module.BusinessObjects;
using OutlookInspired.Tests.Common;
using XAF.Testing;
using XAF.Testing.XAF;

namespace OutlookInspired.Tests.Services{
    public static class CustomerExtensions{
        public static IObservable<Unit> AssertCustomerListView(this XafApplication application,string view, string viewVariant) 
            => application.AssertNavigation(view, viewVariant, source => source.AssertCustomerListView(application,viewVariant),application.CanNavigate(view).ToUnit())
                .FilterListViews(application)
            ;

        private static IObservable<Unit> AssertCustomerListView(this IObservable<Frame> source, XafApplication application, string viewVariant){
            var customerTabControl = application.AssertTabbedGroup(typeof(Customer),5);
            return source.AssertDashboardMasterDetail(
                    frame => customerTabControl.AssertCustomerDetailView(frame).ToUnit(),assert:frame => frame.AssertAction())
                .Merge(customerTabControl.IgnoreElements().To<Frame>())
                .ReplayFirstTake()
                .If(_ => viewVariant == "CustomerListView", frame => 
                    frame.AssertDashboardViewGridControlDetailViewObjects(nameof(Customer.RecentOrders), nameof(Customer.Employees)), frame => frame.Observe())
                .ToUnit();
        }

        internal static IObservable<Frame> AssertCustomerDetailView(this IObservable<ITabControlProvider> source, Frame frame)
            => source.AssertNestedCustomerEmployee(frame, 1).IgnoreElements()
                .Concat(source.AssertNestedQuote(frame, 2)).IgnoreElements()
                .Concat(source.AssertNestedCustomerStore(frame)).IgnoreElements()
                .Concat(source.AssertNestedOrder(frame, 4))
                .ReplayFirstTake();

        internal static IObservable<Frame> AssertNestedCustomerEmployee(this IObservable<ITabControlProvider> source, Frame nestedFrame,int tabIndex)
            => source.AssertNestedListView(nestedFrame, typeof(CustomerEmployee),tabIndex, existingObjectDetailViewFrame => 
                existingObjectDetailViewFrame.AssertRootCustomerEmployee(), frame =>frame.AssertAction(nestedFrame) );
        
        internal static IObservable<Frame> AssertNestedCustomerEmployee(this Frame nestedFrame) 
            => nestedFrame.AssertNestedListView(typeof(CustomerEmployee), existingObjectDetailViewFrame =>
                    existingObjectDetailViewFrame.AssertRootCustomerEmployee(),
                frame => frame.AssertAction(nestedFrame));

        private static IObservable<Frame> AssertNestedCustomerStore(this IObservable<ITabControlProvider> source,Frame nestedFrame){
            var customerStoreTabbedGroup = nestedFrame.Application.AssertTabbedGroup(typeof(CustomerStore), 3);
            return source.AssertNestedListView(nestedFrame, typeof(CustomerStore), selectedTabPageIndex: 3,
                frame => customerStoreTabbedGroup.AssertRootCustomerStore(frame).ToUnit(),frame => frame.AssertAction())
                .Merge(customerStoreTabbedGroup.To<Frame>().IgnoreElements()).ReplayFirstTake();
        }

        private static IObservable<Frame> AssertRootCustomerStore(this IObservable<ITabControlProvider> source, Frame frame) 
            => frame.Defer(() => frame.AssertNestedCustomerEmployee().IgnoreElements()
                    .ConcatDefer(() => source.AssertNestedOrder(frame, 1).IgnoreElements())
                    .Concat(source.Select(provider => provider).AssertNestedQuote(frame, 2)))
                .Merge(source.IgnoreElements().To<Frame>()).ReplayFirstTake();

        static IObservable<Unit> AssertRootCustomerEmployee(this  Frame frame)
            => frame.AssertNestedListView(typeof(CustomerCommunication),assert:frame1 => frame1.AssertAction()).ToUnit();
        
        
    }
}