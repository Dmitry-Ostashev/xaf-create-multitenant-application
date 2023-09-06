﻿using OutlookInspired.Module.BusinessObjects;

namespace OutlookInspired.Module.Model.HideViewActions{
    public class MapsUpdater : HideViewActionsUpdater{
        protected override string[] ActionIds() 
            => new[]{ "OpenObject" };

        protected override string[] ViewIds() 
            => new[]{
                Customer.MapsDetailView,Employee.MapsDetailView,
                Product.MapsDetailView, Order.MapsDetailView,Quote.MapsDetailView
            };
    }
}