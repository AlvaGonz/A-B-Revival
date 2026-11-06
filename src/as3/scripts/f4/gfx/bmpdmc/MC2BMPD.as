package f4.gfx.bmpdmc
{
   import f4.data.InitData;
   import flash.display.BitmapData;
   import flash.display.DisplayObject;
   import flash.display.MovieClip;
   import flash.filters.ColorMatrixFilter;
   import flash.geom.Matrix;
   import flash.geom.Rectangle;
   import fullshit.render.ColorMatrixAdd;
   
   public class MC2BMPD
   {
      
      public function MC2BMPD()
      {
         super();
      }
      
      public static function mc2bmpd(param1:MovieClip, param2:String) : BMPDMCData1
      {
         var _loc7_:Rectangle = null;
         var _loc8_:Number = NaN;
         var _loc3_:int = param1.totalFrames - 1;
         var _loc4_:int = Math.ceil(Math.sqrt(_loc3_));
         var _loc5_:MovieClip = param1.getChildByName("RECT") as MovieClip;
         var _loc6_:Number = Math.max(_loc5_.width,_loc5_.height);
         _loc5_.width = _loc5_.height = Math.round(_loc6_);
         if(_loc5_)
         {
            _loc7_ = new Rectangle(_loc5_.x,_loc5_.y,_loc5_.width,_loc5_.height);
            _loc8_ = _loc5_.x;
         }
         else
         {
            _loc7_ = new Rectangle(0,0,param1.width,param1.height);
            _loc8_ = 0;
         }
         var _loc9_:Matrix = new Matrix(1,0,0,1,-_loc7_.x,-_loc7_.y);
         var _loc10_:BitmapData = new BitmapData(_loc4_ * _loc7_.width,_loc4_ * _loc7_.height,true,0);
         var _loc11_:int = -1;
         _loc7_.x = _loc7_.y = 0;
         while(++_loc11_ < _loc3_)
         {
            param1.gotoAndStop(_loc11_ + 1);
            _loc10_.draw(param1,_loc9_,null,null,_loc7_,true);
            _loc7_.x += _loc7_.width;
            _loc9_.tx += _loc7_.width;
            if(_loc7_.x + _loc7_.width > _loc10_.width)
            {
               _loc7_.x = 0;
               _loc7_.y += _loc7_.height;
               _loc9_.tx = -_loc8_;
               _loc9_.ty += _loc7_.height;
            }
         }
         _loc7_.x = _loc7_.y = 0;
         return new BMPDMCData1(_loc3_,_loc7_,_loc10_,param2);
      }
      
      public static function mc2bmpd2Single(param1:MovieClip, param2:String, param3:Boolean, param4:int, param5:int, param6:Boolean = true) : BMPDMCData2
      {
         var _loc9_:int = 0;
         var _loc11_:DisplayObject = null;
         var _loc12_:String = null;
         var _loc16_:Rectangle = null;
         var _loc20_:ColorMatrixAdd = null;
         var _loc7_:MovieClip = param1.getChildByName("SHOW") as MovieClip;
         if(param6)
         {
            _loc20_ = new ColorMatrixAdd(new ColorMatrixFilter());
            _loc20_.setBrightness(-0.25);
            _loc20_.setContrast(-0.1);
            _loc20_.setSaturation(0);
            _loc20_.setHue(param5 / 180 * Math.PI);
            _loc20_.setColor();
            _loc7_.filters = [_loc20_.color];
         }
         var _loc8_:int = InitData.RENDERDATA.FACE;
         var _loc10_:int = _loc7_.numChildren;
         _loc9_ = 0;
         while(_loc9_ < _loc10_)
         {
            _loc11_ = _loc7_.getChildAt(_loc9_);
            _loc12_ = _loc11_.name;
            if(_loc12_.charAt(0) == "L")
            {
               _loc11_.visible = false;
            }
            else if(_loc12_.charAt(0) == "H")
            {
               _loc11_.visible = false;
            }
            else if(_loc12_.charAt(0) == "F")
            {
               _loc11_.visible = false;
            }
            else if(_loc12_.charAt(0) == "P")
            {
               _loc11_.visible = false;
            }
            _loc9_++;
         }
         if(param4 == 1)
         {
            _loc7_.scaleX = -1;
         }
         if(!param3)
         {
            _loc8_ = 1;
         }
         var _loc13_:int = Math.ceil(Math.sqrt(_loc8_));
         var _loc14_:MovieClip = param1.getChildByName("RECT") as MovieClip;
         var _loc15_:Number = Math.max(_loc14_.width,_loc14_.height);
         _loc14_.width = _loc14_.height = Math.round(_loc15_);
         _loc16_ = new Rectangle(_loc14_.x,_loc14_.y,_loc14_.width,_loc14_.height);
         var _loc17_:Matrix = new Matrix();
         var _loc18_:BitmapData = new BitmapData(_loc13_ * _loc16_.width,_loc13_ * _loc16_.height,true,0);
         _loc9_ = -1;
         var _loc19_:Number = InitData.RENDERDATA.PIDFACE / Math.PI * 180;
         _loc7_.gotoAndStop(1);
         _loc17_.tx = _loc16_.width / 2;
         _loc17_.ty = _loc16_.height / 2;
         _loc16_.x = 0;
         _loc16_.y = 0;
         while(++_loc9_ < _loc8_)
         {
            _loc18_.draw(param1,_loc17_,null,null,_loc16_,true);
            _loc16_.x += _loc16_.width;
            _loc17_.tx += _loc16_.width;
            if(_loc16_.x + _loc16_.width > _loc18_.width)
            {
               _loc16_.x = 0;
               _loc16_.y += _loc16_.height;
               _loc17_.tx = _loc16_.width / 2;
               _loc17_.ty += _loc16_.height;
            }
            _loc7_.rotation += _loc19_;
         }
         return new BMPDMCData2(param2,new BMPDMCData1(_loc8_,_loc16_,_loc18_,param2));
      }
      
      public static function mc2bmpd2Ani(param1:MovieClip, param2:String, param3:int) : BMPDMCData2
      {
         var _loc6_:int = 0;
         var _loc8_:DisplayObject = null;
         var _loc9_:String = null;
         var _loc13_:Rectangle = null;
         var _loc4_:MovieClip = param1.getChildByName("SHOW") as MovieClip;
         var _loc5_:int = _loc4_.totalFrames;
         var _loc7_:int = _loc4_.numChildren;
         _loc6_ = 0;
         while(_loc6_ < _loc7_)
         {
            _loc8_ = _loc4_.getChildAt(_loc6_);
            _loc9_ = _loc8_.name;
            if(_loc9_.charAt(0) == "L")
            {
               _loc8_.visible = false;
            }
            else if(_loc9_.charAt(0) == "H")
            {
               _loc8_.visible = false;
            }
            else if(_loc9_.charAt(0) == "F")
            {
               _loc8_.visible = false;
            }
            else if(_loc9_.charAt(0) == "P")
            {
               _loc8_.visible = false;
            }
            _loc6_++;
         }
         if(param3 == 1)
         {
            _loc4_.scaleX = -1;
         }
         var _loc10_:int = Math.ceil(Math.sqrt(_loc5_));
         var _loc11_:MovieClip = param1.getChildByName("RECT") as MovieClip;
         var _loc12_:Number = Math.max(_loc11_.width,_loc11_.height);
         _loc11_.width = _loc11_.height = Math.round(_loc12_);
         _loc13_ = new Rectangle(_loc11_.x,_loc11_.y,_loc11_.width,_loc11_.height);
         var _loc14_:Matrix = new Matrix();
         var _loc15_:BitmapData = new BitmapData(_loc10_ * _loc13_.width,_loc10_ * _loc13_.height,true,0);
         _loc6_ = -1;
         _loc14_.tx = _loc13_.width / 2;
         _loc14_.ty = _loc13_.height / 2;
         _loc13_.x = 0;
         _loc13_.y = 0;
         while(++_loc6_ < _loc5_)
         {
            _loc4_.gotoAndStop(_loc6_ + 1);
            _loc15_.draw(param1,_loc14_,null,null,_loc13_,true);
            _loc13_.x += _loc13_.width;
            _loc14_.tx += _loc13_.width;
            if(_loc13_.x + _loc13_.width > _loc15_.width)
            {
               _loc13_.x = 0;
               _loc13_.y += _loc13_.height;
               _loc14_.tx = _loc13_.width / 2;
               _loc14_.ty += _loc13_.height;
            }
         }
         return new BMPDMCData2(param2,new BMPDMCData1(_loc5_,_loc13_,_loc15_,param2));
      }
      
      public static function makeBmpd(param1:MovieClip) : BitmapData
      {
         var _loc2_:BitmapData = new BitmapData(param1.width,param1.height,true,0);
         _loc2_.draw(param1);
         return _loc2_;
      }
   }
}

