shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

void vertex() {
	POSITION = vec4(VERTEX.xy,1.0, 1.0);
}

global uniform sampler2DArray world_colors: filter_nearest;
global uniform sampler2DArray world_opacity: filter_nearest;

const int RENDER_DISTANCE = 64*2;
const float CHUNK_SIZE = 4.;
const float CHUNK_SIZE_INV = 0.25;

vec3 GetColor(ivec3 pos){
	ivec3 size=textureSize(world_colors,0);
	pos+=size>>1;// pos+=size/2;
	vec4 fdatas=texelFetch(world_colors,pos,0);
	
	return fdatas.rgb;
}

uvec2 GetChunkOccupancy(ivec3 pos){
	ivec3 size=textureSize(world_colors,0);
	pos+=size>>3;
	vec4 fdata=texelFetch(world_opacity,pos,0);
	uint data1=floatBitsToUint(fdata.r);
	uint data2=floatBitsToUint(fdata.g);
	return uvec2(data1,data2);
}

bool GetOccupancyBit(uvec2 chunk,vec3 pos){
	uvec3 upos= uvec3(pos);//&uvec3(3u)
	uint index= upos.x|(upos.y<<2u)|(upos.z<<4u);
	bool topHalf= 32u<=index;
	index-=uint(topHalf)<<5u;
	uint data=(topHalf)?chunk.y:chunk.x;
	return (data & (1u<<index))!=0u;
}

vec3 GetMove(vec3 itr){
	bvec3 pon=lessThan(itr.xyz,itr.yzx);    
	bvec3 move;
    move.x=pon.x&& !pon.z;
    move.y=pon.y&& !pon.x;
    move.z=!(move.x||move.y);
	return vec3(move);
}

// z is upward
// x is the view horizontal rotation axis
// y is the vertical cam rotation axis and view rotation axis
// z is the horizontal cam rotation axis
void fragment(){
    vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, 1.0);
	vec4 view = INV_VIEW_MATRIX *(INV_PROJECTION_MATRIX * vec4(ndc, 1.0));
	vec3 ray= vec3(view.xyz/ view.w);
	
	vec4 preCamPos=INV_VIEW_MATRIX *vec4(0.,0.,0.,1.);
	vec3 camPos=vec3(preCamPos.xyz/preCamPos.w);
	ray=ray-camPos;
	ray=ray.xzy;//*0.1;
	camPos=camPos.xzy;//*0.1;
	
    /////// doing the voxel traversal from here /////////
    vec3 raySign= sign(ray);
	vec3 raySign01= vec3(greaterThanEqual(ray,vec3(0)));
    
    ray = abs(ray);
    vec3  rayInv=1./ray;
	
	vec3 camPos2=camPos*CHUNK_SIZE_INV;
	//world level
	vec3 pos2= floor(camPos2);
	vec3 itr2=(raySign01 +pos2-camPos2) *rayInv*raySign;
	
	
	vec3 camPos1=(camPos2-pos2)*CHUNK_SIZE;
	//chunk level
	vec3 pos=floor(camPos1);
	vec3 itr=(raySign01 +pos-camPos1) *rayInv*raySign;
	vec3 move=vec3(0);
	
	bool hasHit =false;
	bool skipped=false;
	
	uvec2 chunk =GetChunkOccupancy(ivec3(pos2));
	for(int step = 0; step < RENDER_DISTANCE; step++){
		
		if(skipped||any(lessThan(pos,vec3(0)))||any(greaterThanEqual(pos,vec3(CHUNK_SIZE)))){
			vec3 move2=GetMove(itr2);
			
			itr2+= move2*rayInv;
			pos2+= move2*raySign;
			chunk= GetChunkOccupancy(ivec3(pos2));
			pos -= move2*raySign*CHUNK_SIZE;
			if(chunk.x==0u && chunk.y==0u){
				skipped=true;
				continue;
			}else if(skipped){
				skipped=false;
				vec3 mini = (pos2-camPos2 + 1.-raySign01) * rayInv*raySign;
				float d = max (mini.x, max (mini.y, mini.z));
				vec3 intersect = camPos2 + ray*d*raySign;
				
				vec3 uv3d = (intersect - pos2)*CHUNK_SIZE;
				uv3d=clamp(uv3d,vec3(0.00001),vec3(3.99999));
				pos=floor(uv3d);
				itr=(pos-uv3d +raySign01 ) * rayInv*raySign;
				move=move2;
			}
		}   
		hasHit=GetOccupancyBit(chunk,pos);
		if(hasHit){
			break;
		}
		
		move=GetMove(itr);
		itr+=move*rayInv;
		pos+=move*raySign;
		
	}
	
	
	if(hasHit){
		vec3 rpos=pos2*CHUNK_SIZE+pos;
		vec3 hitcol=GetColor(ivec3(rpos));
		vec3 lascol=GetColor(ivec3(rpos-move*raySign));
		vec3 col= hitcol*lascol;
		
		vec3 lb=move*normalize(ray);
		float lambert=(lb.x+lb.y+lb.z)*0.3+0.7;
		
		ALBEDO = col*lambert;
		
		vec3 mini = (rpos-camPos + 1.-raySign01)*rayInv*raySign;
        float d = max (mini.x, max (mini.y, mini.z));
        vec3 intersect = camPos + ray*d*raySign;
		if (rpos == floor(camPos)){ // Handle edge case where camera origin is inside of block
               intersect = camPos;
		}
		
		vec4 yeet=PROJECTION_MATRIX*VIEW_MATRIX*vec4(intersect.xzy,1.);
		DEPTH=yeet.z/yeet.w;
    }else{
		ALPHA=0.;
	}
}