package fullshit.tool
{
   public class RGBHSLHSV
   {
      
      private const E:Number = 0.000001;
      
      private var _rgb:uint;
      
      private var _H:Number;
      
      private var _hslS:Number;
      
      private var _hslL:Number;
      
      private var _hsvS:Number;
      
      private var _hsvV:Number;
      
      public function RGBHSLHSV(param1:uint = 255)
      {
         super();
         this._rgb = param1;
         this.RGB2HSL();
         this.RGB2HSV();
      }
      
      public function get rgb() : uint
      {
         return this._rgb;
      }
      
      public function changeRGB(param1:uint) : void
      {
         this._rgb = param1;
         this.RGB2HSL();
         this.RGB2HSV();
      }
      
      public function changeRGBD(param1:uint) : void
      {
         this._rgb = param1;
      }
      
      public function set H(param1:Number) : void
      {
         var _loc2_:Number = param1;
         while(_loc2_ > 360)
         {
            _loc2_ -= 360;
         }
         while(_loc2_ < 0)
         {
            _loc2_ += 360;
         }
         this._H = _loc2_;
      }
      
      public function set hslS(param1:Number) : void
      {
         var _loc2_:Number = param1;
         if(_loc2_ > 1)
         {
            _loc2_ = 1;
         }
         if(_loc2_ < 0)
         {
            _loc2_ = 0;
         }
         this._hslS = _loc2_;
      }
      
      public function set hslL(param1:Number) : void
      {
         var _loc2_:Number = param1;
         if(_loc2_ > 1)
         {
            _loc2_ = 1;
         }
         if(_loc2_ < 0)
         {
            _loc2_ = 0;
         }
         this._hslL = _loc2_;
      }
      
      public function set hsvS(param1:Number) : void
      {
         var _loc2_:Number = param1;
         if(_loc2_ > 1)
         {
            _loc2_ = 1;
         }
         if(_loc2_ < 0)
         {
            _loc2_ = 0;
         }
         this._hsvS = _loc2_;
      }
      
      public function set hsvV(param1:Number) : void
      {
         var _loc2_:Number = param1;
         if(_loc2_ > 1)
         {
            _loc2_ = 1;
         }
         if(_loc2_ < 0)
         {
            _loc2_ = 0;
         }
         this._hsvV = _loc2_;
      }
      
      public function get H() : Number
      {
         return this._H;
      }
      
      public function get hslS() : Number
      {
         return this._hslS;
      }
      
      public function get hslL() : Number
      {
         return this._hslL;
      }
      
      public function get hsvS() : Number
      {
         return this._hsvS;
      }
      
      public function get hsvV() : Number
      {
         return this._hsvV;
      }
      
      public function RGB2HSL() : void
      {
         var _loc1_:Number = this.r / 255;
         var _loc2_:Number = this.g / 255;
         var _loc3_:Number = this.b / 255;
         var _loc4_:Number = this.maxRGB / 255;
         var _loc5_:Number = this.minRGB / 255;
         this.RGB2H(_loc1_,_loc2_,_loc3_,_loc4_,_loc5_);
         this._hslL = (_loc4_ + _loc5_) * 0.5;
         if(_loc4_ - _loc5_ < this.E)
         {
            this._hslS = 0;
         }
         else if(this._hslL <= 0.5)
         {
            this._hslS = (_loc4_ - _loc5_) / (2 * this._hslL);
         }
         else if(this._hslL > 0.5)
         {
            this._hslS = (_loc4_ - _loc5_) / (2 - 2 * this._hslL);
         }
      }
      
      private function RGB2H(param1:Number, param2:Number, param3:Number, param4:Number, param5:Number) : void
      {
         if(param4 - param5 < this.E)
         {
            this._H = 0;
         }
         else if(param4 == param1)
         {
            this._H = (60 * (param2 - param3) / (param4 - param5) + 360) % 360;
         }
         else if(param4 == param2)
         {
            this._H = 60 * (param3 - param1) / (param4 - param5) + 120;
         }
         else if(param4 == param3)
         {
            this._H = 60 * (param1 - param2) / (param4 - param5) + 240;
         }
      }
      
      public function HSL2RGB() : void
      {
         var _loc1_:Number = NaN;
         var _loc2_:Number = NaN;
         var _loc3_:Number = NaN;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         if(this._hslL < 0.5)
         {
            _loc1_ = this._hslL * (1 + this._hslS);
         }
         else if(this._hslL >= 0.5)
         {
            _loc1_ = this._hslL + this._hslS - this._hslL * this._hslS;
         }
         _loc2_ = 2 * this._hslL - _loc1_;
         _loc3_ = this._H / 360;
         _loc4_ = _loc3_ + 1 / 3;
         _loc5_ = _loc3_;
         _loc6_ = _loc3_ - 1 / 3;
         _loc4_ = this.colorIn01(_loc4_);
         _loc5_ = this.colorIn01(_loc5_);
         _loc6_ = this.colorIn01(_loc6_);
         this._rgb = int(this.colorBack(_loc4_,_loc2_,_loc1_) * 255) << 16 | int(this.colorBack(_loc5_,_loc2_,_loc1_) * 255) << 8 | int(this.colorBack(_loc6_,_loc2_,_loc1_) * 255);
      }
      
      private function colorBack(param1:Number, param2:Number, param3:Number) : Number
      {
         var _loc4_:Number = 0;
         if(param1 < 1 / 6)
         {
            _loc4_ = param2 + (param3 - param2) * 6 * param1;
         }
         else if(param1 >= 1 / 6 && param1 < 0.5)
         {
            _loc4_ = param3;
         }
         else if(param1 >= 0.5 && param1 < 2 / 3)
         {
            _loc4_ = param2 + (param3 - param2) * 6 * (2 / 3 - param1);
         }
         else
         {
            _loc4_ = param2;
         }
         return _loc4_;
      }
      
      private function colorIn01(param1:Number) : Number
      {
         var _loc2_:Number = param1;
         if(_loc2_ < 0)
         {
            _loc2_ += 1;
         }
         else if(_loc2_ > 1)
         {
            _loc2_--;
         }
         return _loc2_;
      }
      
      public function HSV2RGB() : void
      {
         var _loc6_:uint = 0;
         var _loc7_:uint = 0;
         var _loc8_:uint = 0;
         var _loc1_:int = int(this._H / 60) % 6;
         var _loc2_:Number = this._H / 60 - int(this._H / 60);
         var _loc3_:Number = this._hsvV * (1 - this._hsvS);
         var _loc4_:Number = this._hsvV * (1 - _loc2_ * this._hsvS);
         var _loc5_:Number = this._hsvV * (1 - (1 - _loc2_) * this._hsvS);
         switch(_loc1_)
         {
            case 0:
               _loc6_ = this._hsvV * 255;
               _loc7_ = _loc5_ * 255;
               _loc8_ = _loc3_ * 255;
               break;
            case 1:
               _loc6_ = _loc4_ * 255;
               _loc7_ = this._hsvV * 255;
               _loc8_ = _loc3_ * 255;
               break;
            case 2:
               _loc6_ = _loc3_ * 255;
               _loc7_ = this._hsvV * 255;
               _loc8_ = _loc5_ * 255;
               break;
            case 3:
               _loc6_ = _loc3_ * 255;
               _loc7_ = _loc4_ * 255;
               _loc8_ = this._hsvV * 255;
               break;
            case 4:
               _loc6_ = _loc5_ * 255;
               _loc7_ = _loc3_ * 255;
               _loc8_ = this._hsvV * 255;
               break;
            case 5:
               _loc6_ = this._hsvV * 255;
               _loc7_ = _loc3_ * 255;
               _loc8_ = _loc4_ * 255;
         }
         this._rgb = _loc6_ << 16 | _loc7_ << 8 | _loc8_;
      }
      
      public function RGB2HSV() : void
      {
         var _loc1_:Number = this.r / 256;
         var _loc2_:Number = this.g / 256;
         var _loc3_:Number = this.b / 256;
         var _loc4_:Number = this.maxRGB / 256;
         var _loc5_:Number = this.minRGB / 256;
         this.RGB2H(_loc1_,_loc2_,_loc3_,_loc4_,_loc5_);
         if(_loc4_ < this.E)
         {
            this._hsvS = 0;
         }
         else
         {
            this._hsvS = 1 - _loc5_ / _loc4_;
         }
         this._hsvV = _loc4_;
      }
      
      public function get r() : uint
      {
         return this._rgb >> 16 & 0xFF;
      }
      
      public function get g() : uint
      {
         return this._rgb >> 8 & 0xFF;
      }
      
      public function get b() : uint
      {
         return this._rgb & 0xFF;
      }
      
      public function get maxRGB() : uint
      {
         return Math.max(this.r,this.g,this.b);
      }
      
      public function get minRGB() : uint
      {
         return Math.min(this.r,this.g,this.b);
      }
   }
}

