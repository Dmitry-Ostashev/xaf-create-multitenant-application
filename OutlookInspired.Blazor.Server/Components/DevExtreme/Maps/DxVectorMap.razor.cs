﻿using System.Text.Json;

namespace OutlookInspired.Blazor.Server.Components.DevExtreme.Maps{
    public class DxVectorMapModel : MapModel<DxVectorMap>{
        public event EventHandler<MapItemSelectedArgs> MapItemSelected;
        public void SelectMapItem(JsonElement item) 
            => MapItemSelected?.Invoke(this, new MapItemSelectedArgs(item));
        public VectorMapOptions Options{ get; set; } = new();
        
        public BaseLayer LayerDatasource{
            get => GetPropertyValue<BaseLayer>();
            set => SetPropertyValue(value);
        }

        
    }
    
    public class VectorMapOptions{
        public int Zoom{ get; set; }
        public string Height{ get; set; } = "100%";
        public string Width{ get; set; } = "100%";
        public string Provider{ get; set; } = "bing";
        public ApiKey ApiKey{ get; set; } = new();
        public List<object> Layers{ get;  } = new(){
            new PredefinedLayer{DataSource = "DevExpress.viz.map.sources.world"}
        };
        public Tooltip Tooltip{ get; set; } = new();
        public double[] Bounds{ get; set; }
        public string[] Attributes{ get; set; }
    }
    
    public class BaseLayer{
        public object DataSource{ get; set; }
    }
    public class PredefinedLayer:BaseLayer{
        public bool HoverEnabled{ get; set; }
    }
    public class Layer:BaseLayer{
        public string SelectionMode{ get; set; } 
        public string Name{ get; set; }
        
        public string ElementType{ get; set; } 
        public string DataField{ get; set; }
        public string[] Palette{ get; init; }
    }
    
    public class Tooltip{
        public bool Enabled{ get; set; }
        public int ZIndex{ get; set; }
    }
}