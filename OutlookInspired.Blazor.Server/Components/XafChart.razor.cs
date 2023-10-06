﻿using System.Linq.Expressions;
using DevExpress.ExpressApp.Blazor.Components.Models;

namespace OutlookInspired.Blazor.Server.Components {
    public class XafChartModel<T>:ComponentModelBase {
        public IEnumerable<T> Data{
            get => GetPropertyValue<IEnumerable<T>>();
            set => SetPropertyValue(value);
        }

        public Expression<Func<T,string>> ArgumentField{ get; set; }
        public Expression<Func<T,decimal>> ValueField{ get; set; }
        public Expression<Func<T,string>> NameField{ get; set; }
    }
}