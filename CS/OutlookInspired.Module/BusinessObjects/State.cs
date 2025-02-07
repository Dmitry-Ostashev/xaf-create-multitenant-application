﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;


namespace OutlookInspired.Module.BusinessObjects{
    [DefaultProperty(nameof(LongName))]
    public class State:OutlookInspiredBaseObject{
        [MaxLength(255)]
        public virtual string LongName{ get; set; }
        public virtual StateEnum ShortName{ get; set; }
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
            DetailViewImageEditorMode = ImageEditorMode.PictureEdit)]
        public virtual byte[] SmallFlag{ get; set; }
        [ImageEditor(ListViewImageEditorMode = ImageEditorMode.PictureEdit,
            DetailViewImageEditorMode = ImageEditorMode.PictureEdit)]
        public virtual byte[] LargeFlag{ get; set; }
        
    }
    
    public enum StateEnum {
        CA=1, AR, AL, AK, AZ, CO, CT, DE, DC, FL, GA, HI, ID, IL, IN, IA, KS, KY, LA, ME, MD, MA, MI, MN, MS, MO, MT, NE, NV, NH, NJ, NM, NY, NC, OH, OK, OR, PA, RI, SC, SD, TN, TX, UT, VT, VA, WA, WV, WI, WY, ND
    }
}