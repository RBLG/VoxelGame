shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

void vertex() {
	//POSITION = vec4(VERTEX, 1.0);
	POSITION = vec4(VERTEX.xy,1.0, 1.0);
}

struct Voxel{
	bool opaque;
	vec3 color;
	vec2 uv;
};

global uniform sampler2DArray world_colors: filter_nearest;
global uniform sampler2DArray world_opacity: filter_nearest;

const int RENDER_DISTANCE = 64*2;
const int CHUNK_SIZE = 4;
const float CHUNK_SIZE_INV = 0.25;

Voxel getVoxel(ivec3 pos){
	ivec3 size=textureSize(world_colors,0);
	pos+=size>>1;// pos+=size/2;
	vec4 fdatas=texelFetch(world_colors,pos,0);
	
	bool opacity= fdatas.a!=0.;
	vec3 col =fdatas.rgb;
	vec2 uv = vec2(4,4);
	return Voxel(opacity,col,uv);
}

uvec2 GetChunkOccupancy(ivec3 pos){
	ivec3 size=textureSize(world_colors,0);
	pos+=size>>3;
	vec4 fdata=texelFetch(world_opacity,pos,0);
	uint data1=floatBitsToUint(fdata.r);
	uint data2=floatBitsToUint(fdata.g);
	return uvec2(data1,data2);
}

bool GetOccupancyBit(uvec2 chunk,ivec3 pos){
	uvec3 upos= uvec3(pos);//&uvec3(3u)
	uint index= upos.x|(upos.y<<2u)|(upos.z<<4u);
	bool topHalf= 32u<=index;
	index-=uint(topHalf)<<5u;
	uint data=(topHalf)?chunk.y:chunk.x;
	return (data & (1u<<index))!=0u;
}

bvec3 GetMove(vec3 itr){
	bvec3 pon=lessThan(itr.xyz,itr.yzx);    
	bvec3 move;
    move.x=pon.x&& !pon.z;
    move.y=pon.y&& !pon.x;
    move.z=!(move.x||move.y);
	return move;
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
    ivec3 iRaySign = ivec3(raySign);
	bvec3 bRaySign= greaterThanEqual(ray,vec3(0));
    
    ray = abs(ray);
    vec3  rayInv=1./ray;
	
	vec3 camWPos=camPos*CHUNK_SIZE_INV;
	vec3 floorCamWPos=floor(camWPos);
	vec3 fractCamWPos=camWPos-floorCamWPos;
	
	vec3 camCPos=fractCamWPos*float(CHUNK_SIZE);
	vec3 floorCamCPos=floor(camCPos);
	vec3 fractCamCPos=camCPos-floorCamCPos;
	
	//world level
	ivec3 wpos= ivec3(floorCamWPos);
	vec3 witr=(vec3(bRaySign)-fractCamWPos) * rayInv*raySign;
	
	//chunk level
	ivec3 cpos=ivec3(floorCamCPos);
	vec3 citr=(vec3(bRaySign)-fractCamCPos) * rayInv*raySign;
	bvec3 cmove=bvec3(false);
	
	bool hasHit=false;
	bool skipped=false;
	
	uvec2 chunk=GetChunkOccupancy(wpos);
	for(int step = 0; step < RENDER_DISTANCE; step++){
		
		
		if(skipped||any(lessThan(cpos,ivec3(0)))||any(greaterThanEqual(cpos,ivec3(CHUNK_SIZE)))){
			bvec3 wmove=GetMove(witr);
			
			witr+=vec3(wmove)*rayInv;
			wpos+=ivec3(wmove)*iRaySign;
			chunk=GetChunkOccupancy(wpos);
			cpos-=ivec3(wmove)*iRaySign*CHUNK_SIZE;
			if(all(equal(chunk,uvec2(0)))){
				skipped=true;
				continue;
			}else if(skipped){
				skipped=false;
				vec3 mini = (vec3(wpos)-camWPos + vec3(not(bRaySign))) * rayInv*raySign;
				float d = max (mini.x, max (mini.y, mini.z));
				vec3 intersect = camWPos + ray*d*raySign;
				
				vec3 uv3d = (intersect - vec3(wpos))*float(CHUNK_SIZE);
				uv3d=clamp(uv3d,vec3(0.00001),vec3(3.99999));
				cpos=ivec3(floor(uv3d));
				citr=(floor(uv3d)-uv3d +vec3(bRaySign) ) * rayInv*raySign;
				cmove=wmove;
			}
		}   
		hasHit=GetOccupancyBit(chunk,cpos);
		if(hasHit){
			break;
		}
		
		cmove=GetMove(citr);
		citr+=vec3(cmove)*rayInv;
		cpos+=ivec3(cmove)*iRaySign;
		
	}
	
	
	
	vec3 col=vec3(0);
	if(hasHit){
		Voxel hitvox=getVoxel(wpos*CHUNK_SIZE+cpos);
		Voxel lasvox=getVoxel(wpos*CHUNK_SIZE+cpos-ivec3(cmove)*iRaySign);
		col=hitvox.color*lasvox.color;
		
		vec3 lb=vec3(cmove)*normalize(ray);
		float lambert=(lb.x+lb.y+lb.z)*0.3+0.7;
		
		ALBEDO = col*lambert;
		//ALBEDO=1.-fract(vec3(wpos*CHUNK_SIZE+cpos)*0.03);
		
		vec3 mini = ((vec3(wpos*CHUNK_SIZE+cpos)-camPos) + vec3(not(bRaySign)))*rayInv*raySign;
        float d = max (mini.x, max (mini.y, mini.z));
        vec3 intersect = camPos + ray*d*raySign;
		if (vec3(wpos*CHUNK_SIZE+cpos) == floor(camPos)){ // Handle edge case where camera origin is inside of block
               intersect = camPos;
		}
		
		vec4 yeet=PROJECTION_MATRIX*VIEW_MATRIX*vec4(intersect.xzy,1.);
		DEPTH=yeet.z/yeet.w;
    }else{
		//DEPTH=1.11;
		ALPHA=0.;
	}
    
    // output to screen
    
    
}