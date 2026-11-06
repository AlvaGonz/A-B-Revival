package fullshit.tool
{
   import flash.display.BitmapData;
   import flash.filters.BlurFilter;
   import flash.geom.ColorTransform;
   import flash.geom.Matrix;
   import flash.geom.Point;
   import flash.geom.Rectangle;
   
   public class SimpleGlowFilter
   {
      
      public var colorTransform:ColorTransform;
      
      public var blur:BlurFilter;
      
      private var _downSampleNumber:Number;
      
      private var _matrix:Matrix;
      
      private var _matrixI:Matrix;
      
      private var _point:Point;
      
      private var _source:BitmapData;
      
      private var _downSampled:BitmapData;
      
      private var _rect:Rectangle;
      
      private var _downRect:Rectangle;
      
      private var _colorTransformFirst:ColorTransform;
      
      public var ctNum:Number = 1;
      
      public function SimpleGlowFilter(param1:BitmapData, param2:Number)
      {
         super();
         this._source = param1;
         this._downSampleNumber = param2;
         this._matrix = new Matrix();
         this._point = new Point(0,0);
         this.colorTransform = new ColorTransform();
         this._colorTransformFirst = new ColorTransform();
         this.blur = new BlurFilter(8,8,1);
         this._matrix.scale(this._downSampleNumber,this._downSampleNumber);
         var _loc3_:uint = this._source.width * this._downSampleNumber;
         var _loc4_:uint = this._source.height * this._downSampleNumber;
         this._downSampled = new BitmapData(_loc3_,_loc4_,true,0);
         this._matrixI = this._matrix.clone();
         this._matrixI.invert();
         this._rect = this._source.rect;
         this._downRect = this._downSampled.rect;
         this.colorTransform.alphaMultiplier = 0.9;
         this.colorTransform.redMultiplier = 1;
         this.colorTransform.greenMultiplier = 1;
         this.colorTransform.blueMultiplier = 1;
         this._colorTransformFirst.alphaMultiplier = 1;
         this._colorTransformFirst.redMultiplier = 1;
         this._colorTransformFirst.greenMultiplier = 1;
         this._colorTransformFirst.blueMultiplier = 1;
      }
      
      public function ctSet() : void
      {
         if(this.ctNum > 1)
         {
            this.ctNum *= 0.98;
            if(this.ctNum < 1)
            {
               this.ctNum = 1;
            }
            else if(this.ctNum > 1.1)
            {
               this.ctNum = 1.1;
            }
            this.colorTransform.redMultiplier = this.ctNum;
            this.colorTransform.greenMultiplier = this.ctNum;
            this.colorTransform.blueMultiplier = this.ctNum;
         }
      }
      
      public function dispose() : void
      {
         this._downSampled.dispose();
      }
      
      public function process() : void
      {
         this.ctSet();
         this._downSampled.colorTransform(this._downRect,this.colorTransform);
         this._downSampled.draw(this._source,this._matrix);
         this._downSampled.applyFilter(this._downSampled,this._downRect,this._point,this.blur);
         this._source.draw(this._downSampled,this._matrixI,null,"add");
      }
   }
}

