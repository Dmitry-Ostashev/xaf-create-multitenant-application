﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Base.General;
using DevExpress.Persistent.Validation;
using OutlookInspired.Module.Attributes;
using OutlookInspired.Module.Features.CloneView;

namespace OutlookInspired.Module.BusinessObjects{
    [Appearance(nameof(StartOn),AppearanceItemType.ViewItem, "1=1",TargetItems = nameof(StartOn),FontStyle = FontStyle.Bold,Context = "Employee_Evaluations_ListView")]
    [Appearance(nameof(Manager),AppearanceItemType.ViewItem, "1=1",TargetItems = nameof(Manager),FontStyle = FontStyle.Bold,Context = EmployeeEvaluationsChildListView)]
    [Appearance(nameof(StartOn)+"_"+EmployeeEvaluationsChildListView,AppearanceItemType.ViewItem, "1=1",TargetItems = nameof(StartOn),FontColor = "Blue",Context = EmployeeEvaluationsChildListView)]
    [Appearance(nameof(Rating),AppearanceItemType.ViewItem, nameof(Rating)+"='"+nameof(EvaluationRating.Good)+"'",TargetItems = "*",FontColor = "Green",Context = "Employee_Evaluations_ListView")]
    [CloneView(CloneViewType.ListView, EmployeeEvaluationsChildListView)]
    [DefaultClassOptions][ImageName("EvaluationYes")][VisibleInReports(false)]
    public class Evaluation :OutlookInspiredBaseObject,IEvent{
        public const string EmployeeEvaluationsChildListView="Employee_Evaluations_ListView_Child";

		public override void OnCreated() {
			base.OnCreated();
			StartOn = DateTime.Now;
			EndOn = StartOn.Value.AddHours(1);
			Color = Color.White;
		}

		[FieldSize(FieldSizeAttribute.Unlimited)]
		public virtual string Description{ get; set; }
		public virtual DateTime? EndOn { get; set; }
		[ImmediatePostData][Browsable(false)]
		public virtual Boolean AllDay { get; set; }
		[Browsable(false)]
		public virtual String Location { get; set; }
		[Browsable(false)]
		public virtual Int32 Label { get; set; }
		[Browsable(false)]
		public virtual Int32 Status { get; set; }
		[Browsable(false)]
		public virtual Int32 Type { get; set; }
		
		[NotMapped, Browsable(false)]
		public virtual String ResourceId{ get; set; }

		[Browsable(false)]
		public Object AppointmentId => ID;
		
		DateTime IEvent.StartOn {
			get => StartOn ?? DateTime.MinValue;
			set => StartOn = value;
		}
		DateTime IEvent.EndOn {
			get => EndOn ?? DateTime.MinValue;
			set => EndOn = value;
		}
		
		[RuleRequiredField]
        public virtual Employee Manager{ get; set; }
        [Browsable(false)]
        public virtual Guid? ManagerId{ get; set; }
        [RuleRequiredField]
        public virtual DateTime? StartOn{ get; set; }
        [RuleRequiredField(DefaultContexts.Save)]
        public virtual Employee Employee{ get; set; }
        [FontSizeDelta(8)]
        public virtual string Subject{ get; set; }
        
        public virtual EvaluationRating Rating{ get; set; }

        [VisibleInListView(false)]
        public virtual Raise Raise{ get; set; }

        [VisibleInListView(false)]
        public virtual Bonus Bonus{ get; set; }
        
        [Browsable(false)]
        public virtual Int32 ColorInt { get; protected set; }

        [NotMapped][Browsable(false)]
        public Color Color {
            get => Color.FromArgb(ColorInt);
            set => ColorInt = value.ToArgb();
        }
    }

    public enum Raise{
        [XafDisplayName("RAISE")]
        [ImageName("EvaluationNo")]
        No,
        [XafDisplayName("RAISE")]
        [ImageName("EvaluationYes")]
        Yes
    }
    public enum Bonus{
        [XafDisplayName("BONUS")]
        [ImageName("EvaluationNo")]
        No,
        [XafDisplayName("BONUS")]
        [ImageName("EvaluationYes")]
        Yes
    }

    public enum EvaluationRating {
        Unset, Good, Average, Poor
    }

}