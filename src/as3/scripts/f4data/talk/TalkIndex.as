package f4data.talk
{
   public class TalkIndex
   {
      
      public var talkBefore:Object;
      
      public var talkAfter:Object;
      
      public var talkData:Object;
      
      public var place:Object;
      
      public var placeIndex:Object;
      
      public function TalkIndex()
      {
         super();
         this.initIndex();
      }
      
      public function initIndex() : void
      {
         this.place = [0,0,0,0];
         this.talkBefore = [];
         this.talkAfter = [];
         this.talkBefore[0] = [2,10,null];
         this.talkAfter[0] = [11,15,null];
         this.talkBefore[1] = [19,40,null];
         this.talkAfter[1];
         this.talkBefore[2] = [50,58,null];
         this.talkAfter[2] = [59,60,null];
         this.talkBefore[3] = [63,78,null];
         this.talkAfter[3] = [79,79,null];
         this.talkBefore[4] = [82,85,null];
         this.talkAfter[4] = [86,86,null];
         this.talkBefore[5] = [89,94,null];
         this.talkAfter[5] = [95,97,null];
         this.talkBefore[6] = [99,110,null];
         this.talkAfter[6];
         this.talkBefore[7] = [113,114,null];
         this.talkAfter[7] = [127,127,null];
         this.talkBefore[8];
         this.talkAfter[8] = [129,129,null];
         this.talkBefore[9] = [134,147,null];
         this.talkAfter[9] = [148,149,null];
         this.talkBefore[10] = [151,156,null];
         this.talkAfter[10] = [157,158,null];
         this.talkBefore[11] = [162,170,null];
         this.talkAfter[11];
         this.talkBefore[12] = [172,175,null];
         this.talkAfter[12] = [176,177,null];
         this.talkBefore[13] = [180,190,null];
         this.talkAfter[13] = [191,193,null];
         this.talkBefore[14] = [194,214,null];
         this.talkAfter[14];
         this.talkBefore[15] = [221,229,null];
         this.talkAfter[15] = [230,231,null];
         this.talkBefore[16] = [234,236,null];
         this.talkAfter[16];
         this.talkBefore[17] = [239,240,null];
         this.talkAfter[17] = [241,241,null];
         this.talkBefore[18] = [244,245,null];
         this.talkAfter[18] = [246,246,null];
         this.talkBefore[19] = [249,250,null];
         this.talkAfter[19] = [251,251,null];
         this.talkBefore[20] = [254,255,null];
         this.talkAfter[20] = [256,256,null];
         this.talkBefore[21] = [259,260,null];
         this.talkAfter[21] = [261,261,null];
         this.talkBefore[22] = [264,265,null];
         this.talkAfter[22] = [266,266,null];
         this.talkBefore[23] = [269,270,null];
         this.talkAfter[23] = [271,271,null];
         this.talkBefore[24] = [275,278,null];
         this.talkAfter[24] = [279,280,null];
         this.talkBefore[25] = [285,291,null];
         this.talkAfter[25] = [292,293,null];
         this.talkBefore[26] = [295,296,null];
         this.talkAfter[26];
         this.talkBefore[27] = [298,299,null];
         this.talkAfter[27];
         this.talkBefore[28] = [301,302,null];
         this.talkAfter[28];
         this.talkBefore[29] = [304,310,null];
         this.talkAfter[29] = [311,314,null];
         this.talkBefore[30] = [316,317,null];
         this.talkAfter[30];
         this.talkBefore[31] = [319,320,null];
         this.talkAfter[31];
         this.talkBefore[32] = [322,323,null];
         this.talkAfter[32];
         this.talkBefore[33] = [325,329,null];
         this.talkAfter[33];
         this.talkBefore[34] = [332,339,null];
         this.talkAfter[34] = [340,342,null];
         this.placeIndex = [[[[383,383,null],[385,385,null]],[[384,384,null],[385,385,null]]],[[[376,376,null],[379,379,null]]],[[[369,369,null],[372,372,null]],[[370,370,null],[372,372,null]],[[371,371,null],[373,373,null]],[[374,374,null],[375,375,null]]],[[[380,380,null],[382,382,null]]]];
         this.talkData = new Object();
      }
      
      public function xmlToObj(param1:XML) : Object
      {
         var _loc3_:XML = null;
         var _loc4_:String = null;
         var _loc2_:Object = new Object();
         for each(_loc3_ in param1.children())
         {
            _loc4_ = _loc3_.toString();
            _loc2_[_loc3_.name()] = _loc4_;
         }
         return _loc2_;
      }
   }
}

