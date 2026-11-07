package f4.gfx
{
   import f4.gfx.bmpdmc.BMPDMCData2;
   import f4.gfx.bmpdmc.MC2BMPD;
   import f4.unit.render.BGMoveSimple;
   import f4data.F4DataHole;
   import f4data.F4DataLinks;
   import f4data.cpx.F4DataCPX;
   import flash.display.MovieClip;
   import flash.utils.Dictionary;
   import fullshit.tool.Attach;
   
   public class GData
   {
      
      public var gdata:Dictionary;
      
      public var mcList:Object;
      
      public function GData()
      {
         super();
         this.gdata = new Dictionary();
         this.initData();
      }
      
      public function initListBG(param1:Array, param2:BGMoveSimple) : void
      {
         var _loc3_:int = 0;
         var _loc4_:Number = NaN;
         var _loc5_:Number = NaN;
         var _loc6_:Number = NaN;
         var _loc7_:Number = NaN;
         var _loc8_:BMPDMCData2 = null;
         var _loc9_:Array = null;
         var _loc10_:int = 0;
         var _loc11_:int = 0;
         var _loc12_:Array = null;
         if(this.mcList)
         {
            this.dispose();
         }
         this.mcList = new Object();
         for each(_loc12_ in param1)
         {
            _loc3_ = int(_loc12_[0]);
            _loc4_ = Number(_loc12_[1]);
            _loc5_ = Number(_loc12_[2]);
            _loc6_ = Number(_loc12_[3]);
            _loc7_ = Number(_loc12_[4]);
            param2.cpx.addBGAt(_loc4_,_loc5_,_loc3_);
            _loc11_ = int(_loc12_.length);
            _loc10_ = 4;
            while(++_loc10_ < _loc11_)
            {
               _loc9_ = _loc12_[_loc10_];
               if(!this.mcList[_loc9_[0]])
               {
                  _loc8_ = MC2BMPD.mc2bmpd2Ani(Attach.attach(_loc9_[0]) as MovieClip,_loc9_[0],0);
                  this.mcList[_loc9_[0]] = _loc8_;
               }
               param2.cpx.addBmpd2dAt((this.mcList[_loc9_[0]] as BMPDMCData2).makeBMPD2D(true),_loc9_[1],_loc9_[2],_loc6_,_loc7_,_loc3_);
            }
         }
      }
      
      public function initListFly(param1:Array) : void
      {
         var _loc2_:F4DataLinks = null;
         for each(_loc2_ in param1)
         {
            this.f4linkSub(_loc2_,_loc2_);
         }
      }
      
      public function dispose() : void
      {
         var _loc1_:BMPDMCData2 = null;
         for each(_loc1_ in this.mcList)
         {
            _loc1_.bmpd1.bmpd.dispose();
         }
      }
      
      public function f4linkSub(param1:F4DataLinks, param2:F4DataLinks) : void
      {
         var _loc3_:BMPDMCData2 = null;
         var _loc4_:F4DataHole = null;
         var _key:String = param1.f4dataorg.mcName + param1.f4dataorg.lr + "_" + param1.f4dataorg.rot + "_" + param2.colorHue;
         if(!this.mcList[_key])
         {
            _loc3_ = MC2BMPD.mc2bmpd2Single(Attach.attach(param1.f4dataorg.mcName) as MovieClip,param1.f4dataorg.mcName,true,param1.f4dataorg.lr,param2.colorHue,true,param1.f4dataorg.rot);
            this.mcList[_key] = _loc3_;
         }
         for each(_loc4_ in param1.f4dataorg.hole)
         {
            if(_loc4_.partf4)
            {
               this.f4linkSub(_loc4_.partf4,param2);
            }
         }
      }
      
      public function f4Blt(param1:String, param2:int) : void
      {
         var _loc3_:BMPDMCData2 = null;
         if(!this.mcList[param1 + "_" + param2])
         {
            _loc3_ = MC2BMPD.mc2bmpd2Single(Attach.attach(param1) as MovieClip,param1,true,2,param2);
            this.mcList[param1 + "_" + param2] = _loc3_;
         }
      }
      
      public function initData() : void
      {
         var _loc1_:BMPDMCData2 = null;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It1") as MovieClip,"It1",0);
         this.gdata["fuel"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It2") as MovieClip,"It2",0);
         this.gdata["money"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It3") as MovieClip,"It3",0);
         this.gdata["life"] = _loc1_;
         _loc1_ = MC2BMPD.mc2bmpd2Ani(Attach.attach("It4") as MovieClip,"It4",0);
         this.gdata["part"] = _loc1_;
      }

      public var atlas:Stage3DTextureAtlas;

      public function initStage3DAtlas(param1:F4DataCPX) : Stage3DTextureAtlas
      {
         if(!this.atlas)
         {
            this.atlas = new Stage3DTextureAtlas();
            this.atlas.buildFromCPX(param1);
         }
         return this.atlas;
      }

      public function getStage3DUV(param1:String) : Object
      {
         return this.atlas ? this.atlas.getUV(param1) : null;
      }

      public function uploadStage3DAtlas(param1:*) : Boolean
      {
         return this.atlas ? this.atlas.upload(param1) : false;
      }
   }
}

import flash.display.BitmapData;
import flash.display.DisplayObject;
import flash.display.MovieClip;
import flash.geom.Matrix;
import flash.geom.Point;
import flash.geom.Rectangle;
import fullshit.tool.Attach;
import f4data.cpx.F4DataCPX;
import f4data.F4DataOrg;
import f4data.F4DataBullet;

class Stage3DTextureAtlas
{
   public static var instance:Stage3DTextureAtlas;

   public var atlasBitmap:BitmapData;
   public var gpuTexture:*;
   public var uvMap:Object;
   public var isUploaded:Boolean = false;
   public var atlasWidth:int = 2048;
   public var atlasHeight:int = 2048;

   private var _curX:int = 2;
   private var _curY:int = 2;
   private var _rowH:int = 0;

   public function Stage3DTextureAtlas()
   {
      super();
      instance = this;
      this.uvMap = {};
   }

   public function getUV(param1:String) : Object
   {
      return this.uvMap ? this.uvMap[param1] : null;
   }

   public function hasUV(param1:String) : Boolean
   {
      return this.uvMap && this.uvMap[param1] != null;
   }

   public function buildFromCPX(param1:F4DataCPX) : void
   {
      if(this.atlasBitmap)
      {
         this.atlasBitmap.dispose();
      }
      this.atlasBitmap = new BitmapData(this.atlasWidth, this.atlasHeight, true, 0x00000000);
      this._curX = 2;
      this._curY = 2;
      this._rowH = 0;

      this.packSymbol("It1");
      this.packSymbol("It2");
      this.packSymbol("It3");
      this.packSymbol("It4");

      if(param1 && param1.defPart)
      {
         for each(var _part:F4DataOrg in param1.defPart)
         {
            if(_part && _part.mcName && !this.hasUV(_part.mcName))
            {
               this.packSymbol(_part.mcName);
            }
         }
      }

      if(param1 && param1.defBullet)
      {
         for each(var _blt:F4DataBullet in param1.defBullet)
         {
            if(_blt && _blt.mcName && _blt.mcName != "normal" && !this.hasUV(_blt.mcName))
            {
               this.packSymbol(_blt.mcName);
            }
         }
      }

      this.packBulletDisc("normal", 8, 0x00CCFF);
   }

   public function packSymbol(symbolName:String, scale:Number = 2.0) : Boolean
   {
      if(!symbolName || this.hasUV(symbolName))
      {
         return false;
      }
      var mc:MovieClip = Attach.attach(symbolName) as MovieClip;
      if(!mc)
      {
         return false;
      }
      var showMc:MovieClip = mc.getChildByName("SHOW") as MovieClip;
      var targetObj:DisplayObject = showMc ? showMc : mc;

      if(showMc)
      {
         var numCh:int = showMc.numChildren;
         for(var i:int = 0; i < numCh; i++)
         {
            var ch:DisplayObject = showMc.getChildAt(i);
            var chName:String = ch.name;
            if(chName && (chName.charAt(0) == "L" || chName.charAt(0) == "H" || chName.charAt(0) == "F" || chName.charAt(0) == "P"))
            {
               ch.visible = false;
            }
         }
      }

      var bounds:Rectangle = targetObj.getBounds(targetObj);
      if(bounds.width <= 0 || bounds.height <= 0)
      {
         bounds = new Rectangle(-16, -16, 32, 32);
      }

      var sw:int = Math.ceil(bounds.width * scale) + 4;
      var sh:int = Math.ceil(bounds.height * scale) + 4;

      if(this._curX + sw + 2 > this.atlasWidth)
      {
         this._curX = 2;
         this._curY += this._rowH + 2;
         this._rowH = 0;
      }
      if(this._curY + sh + 2 > this.atlasHeight)
      {
         return false;
      }

      var mtx:Matrix = new Matrix();
      mtx.translate(-bounds.x, -bounds.y);
      mtx.scale(scale, scale);
      mtx.translate(this._curX + 2, this._curY + 2);

      var clipRect:Rectangle = new Rectangle(this._curX, this._curY, sw + 2, sh + 2);
      this.atlasBitmap.draw(targetObj, mtx, null, null, clipRect, true);

      var u0:Number = Number(this._curX) / Number(this.atlasWidth);
      var v0:Number = Number(this._curY) / Number(this.atlasHeight);
      var u1:Number = Number(this._curX + sw) / Number(this.atlasWidth);
      var v1:Number = Number(this._curY + sh) / Number(this.atlasHeight);

      this.uvMap[symbolName] = {
         "u0": u0,
         "v0": v0,
         "u1": u1,
         "v1": v1,
         "pixelX": this._curX,
         "pixelY": this._curY,
         "pixelW": sw,
         "pixelH": sh,
         "origW": bounds.width,
         "origH": bounds.height,
         "pivotX": -bounds.x * scale + 2,
         "pivotY": -bounds.y * scale + 2
      };

      this._curX += sw + 4;
      if(sh > this._rowH)
      {
         this._rowH = sh;
      }
      return true;
   }

   public function packBulletDisc(symbolName:String, radius:int, color:uint) : void
   {
      var size:int = radius * 4;
      if(this._curX + size + 2 > this.atlasWidth)
      {
         this._curX = 2;
         this._curY += this._rowH + 2;
         this._rowH = 0;
      }
      if(this._curY + size + 2 > this.atlasHeight)
      {
         return;
      }
      var discBmp:BitmapData = new BitmapData(size, size, true, 0x00000000);
      var cx:int = size / 2;
      var cy:int = size / 2;
      for(var py:int = 0; py < size; py++)
      {
         for(var px:int = 0; px < size; px++)
         {
            var dx:int = px - cx;
            var dy:int = py - cy;
            var dist:Number = Math.sqrt(dx * dx + dy * dy);
            if(dist <= radius * 1.5)
            {
               var a:Number = 1.0 - (dist / (radius * 1.5));
               if(a > 1) a = 1;
               if(a < 0) a = 0;
               var alphaInt:uint = uint(a * 255);
               discBmp.setPixel32(px, py, (alphaInt << 24) | (color & 0x00FFFFFF));
            }
         }
      }
      var pt:Point = new Point(this._curX, this._curY);
      this.atlasBitmap.copyPixels(discBmp, discBmp.rect, pt, null, null, true);
      discBmp.dispose();

      var u0:Number = Number(this._curX) / Number(this.atlasWidth);
      var v0:Number = Number(this._curY) / Number(this.atlasHeight);
      var u1:Number = Number(this._curX + size) / Number(this.atlasWidth);
      var v1:Number = Number(this._curY + size) / Number(this.atlasHeight);

      this.uvMap[symbolName] = {
         "u0": u0,
         "v0": v0,
         "u1": u1,
         "v1": v1,
         "pixelX": this._curX,
         "pixelY": this._curY,
         "pixelW": size,
         "pixelH": size,
         "origW": size / 2,
         "origH": size / 2,
         "pivotX": cx,
         "pivotY": cy
      };
      this._curX += size + 4;
      if(size > this._rowH)
      {
         this._rowH = size;
      }
   }

   public function upload(context:*) : Boolean
   {
      if(!context || !this.atlasBitmap)
      {
         return false;
      }
      try
      {
         if(this.gpuTexture)
         {
            this.gpuTexture.dispose();
         }
         this.gpuTexture = context.createTexture(this.atlasWidth, this.atlasHeight, "bgra", false);
         this.gpuTexture.uploadFromBitmapData(this.atlasBitmap);
         this.isUploaded = true;
         return true;
      }
      catch(e:Error)
      {
         this.isUploaded = false;
      }
      return false;
   }

   public function dispose() : void
   {
      if(this.gpuTexture)
      {
         try { this.gpuTexture.dispose(); } catch(e:Error) {}
         this.gpuTexture = null;
      }
      if(this.atlasBitmap)
      {
         this.atlasBitmap.dispose();
         this.atlasBitmap = null;
      }
      this.uvMap = {};
      this.isUploaded = false;
   }
}

