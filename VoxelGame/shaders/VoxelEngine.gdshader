shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

struct Voxel{
	bool opaque;
	vec3 color;
	vec2 uv;
};

uniform sampler3D world_buffer: filter_nearest;

const int RENDER_DISTANCE = 64*8;

const float COLOR_DEPTH_INV=1./255.;
const float UV_DEPTH_INV=1./7.;

const uint mask8b = 255u;//1u<<8u-1u;//0b11111111;
const uint mask3b = 7u;//1u<<3u-1u;//0b111;
//const uint mask2b = 3u;//// 0b11;
const uint mask1b = 1u;//0b1;

Voxel getVoxel(ivec3 pos){
	ivec3 size=textureSize(world_buffer,0);
	pos+=size>>1;// pos+=size/2;
	vec4 fdatas=texelFetch(world_buffer,pos,0);
	uvec4 datas=floatBitsToUint(fdatas);
	uint data=datas.r;
	uint r=(data>>0u  )&mask8b;
	uint g=(data>>8u  )&mask8b;
	uint b=(data>>16u )&mask8b;
	uint a=(data>>24u )&mask1b;
	uint u=(data>>26u )&mask3b;
	uint v=(data>>29u )&mask3b;
	
	bool opacity= a==1u;
	vec3 col= vec3(uvec3(r,g,b))*COLOR_DEPTH_INV;
	vec2 uv =(vec2(uvec2(u,v))-3.)* UV_DEPTH_INV;
	
	return Voxel(opacity,col,uv);
}

// z is upward
// x is the view horizontal rotation axis
// y is the vertical cam rotation axis and view rotation axis
// z is the horizontal cam rotation axis
void fragment()
{
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
    
    ray = abs(ray);
    vec3  rayInv=1./ray;
    bvec3 move;
    Voxel hit= Voxel(false,vec3(0.),vec2(0.));
	Voxel lhit;
    
    vec3  itr= fract(camPos*-raySign)* rayInv;
    ivec3 pos= ivec3(floor(camPos));
    for(int step = 0; step < RENDER_DISTANCE; step++){
        lhit=hit;
		hit=getVoxel(pos);
        if(hit.opaque){break;}
		
		bvec3 pon=lessThan(itr.xyz,itr.yzx);    // pon=Priority Over Next component
        
        move.x=pon.x&& !pon.z;                  // the smaller component is the one that will hit a voxel face first
        move.y=pon.y&& !pon.x;
        move.z=!(move.x||move.y);               // if neither x or y has prio, then default to z, this break triple ties
        
        itr+=vec3(move)*rayInv;                 // update the distance by adding 1 voxel worth of distance
        pos+=ivec3(move)*iRaySign;              // register the move in pos
    }
    vec3 col=vec3(0);
    if(hit.opaque){
		col=hit.color*lhit.color;
		
		vec3 lb=vec3(move)*normalize(ray);
		float lambert=(lb.x+lb.y+lb.z)*0.3+0.7;
		
		ALBEDO = col*lambert;
		
		vec3 mini = ((vec3(pos)-camPos) +0.5- 0.5*vec3(raySign))*rayInv*raySign;
        float d = max (mini.x, max (mini.y, mini.z));
        vec3 intersect = camPos + ray*d*raySign;
		if (vec3(pos) == floor(camPos)){ // Handle edge case where camera origin is inside of block
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