shader_type spatial;
render_mode unshaded;
render_mode depth_draw_always;

void vertex() {
	POSITION = vec4(VERTEX.xy,1.0, 1.0);
}

global uniform uint render_distance=64u;
global uniform ivec3 world_center;

global uniform sampler2DArray world_colors: filter_nearest;
global uniform sampler2DArray world_opacity: filter_nearest;

uniform bool debug_no_light=false;
uniform bool debug_no_color=false;
uniform bool debug_show_steps=false;

const float CHUNK_SIZE = 4.;
const float CHUNK_SIZE_INV = 0.25;

const uvec3 CHUNK_MASK_BASE=uvec3(15u,4369u,65537u);

vec3 GetColor(ivec3 pos){
	pos+=world_center;
	return texelFetch(world_colors,pos,0).rgb;
}

uvec2 GetChunkOccupancy(ivec3 pos){
	pos+=world_center>>2;
	vec4 fdata=texelFetch(world_opacity,pos,0);
	uint data1=floatBitsToUint(fdata.r);
	uint data2=floatBitsToUint(fdata.g);
	return uvec2(data1,data2);
}

bool GetOccupancyBit(uvec2 chunk,vec3 pos){
	uvec3 upos= uvec3(pos);
	uint index= upos.x|(upos.y<<2u)|((upos.z&1u)<<4u);
    uint data = chunk[upos.z>>1u];
	return bool(data & (1u<<index));
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
    vec3 raySign  = sign(ray);
	vec3 raySign01= vec3(greaterThanEqual(ray,vec3(0)));

    ray = abs(ray);
    vec3 rayInv = 1./ray;

	vec3 camPos2= camPos*CHUNK_SIZE_INV;
	//world level
	vec3 pos2= floor(camPos2);
	vec3 itr2= (raySign01 +pos2-camPos2) *rayInv*raySign;


	vec3 camPos1=(camPos2-pos2)*CHUNK_SIZE;
	//chunk level
	vec3 pos=floor(camPos1);
	vec3 itr=(raySign01 +pos-camPos1) *rayInv*raySign;
	vec3 move=vec3(0);

	//precomputing the possible hit occupancy mask
	//vec3 chunkdists= rayInv*4.;
	//float chunkdist= min(chunkdists.x, min(chunkdists.y, chunkdists.z));
	//uvec3 mrc3 = uvec3(max(CHUNK_SIZE-ceil(chunkdist*ray+1.),0.));
	//uvec4 mrc4 = uvec4(mrc3.xy,max(mrc3.z,2u)-2u,min(mrc3.z,2u))*uvec4(1u,4u,16u,16u);
	//uvec4 premask= CHUNK_MASK_BASE.xyzz>>mrc4;


	uvec2 chunk = GetChunkOccupancy(ivec3(pos2));
	bool skipped= !all(bvec2(chunk));
	bool hasHit = false;

	vec3 steps=vec3(0);
	for(uint Step = 0u; Step < render_distance; Step++){
		if(skipped||any(lessThan(pos,vec3(0)))||any(greaterThanEqual(pos,vec3(CHUNK_SIZE)))){
			move = GetMove(itr2);
			itr2+= move*rayInv;
			pos2+= move*raySign;
			pos -= move*raySign*CHUNK_SIZE;
			if(debug_show_steps){steps+=move;}

			chunk= GetChunkOccupancy(ivec3(pos2));
			if(!all(bvec2(chunk))){
				skipped=true;
				continue;
			}else if(skipped){
				skipped=false;
				vec3 mini = (pos2-camPos2 + 1.-raySign01) * rayInv*raySign;
				float d = max (mini.x, max (mini.y, mini.z));

				vec3 uv3d = (camPos2 - pos2 + ray*d*raySign)*CHUNK_SIZE;
				uv3d=clamp(uv3d,vec3(0.00001),vec3(3.99999));
				pos =floor(uv3d);
				itr =(pos-uv3d +raySign01 ) * rayInv*raySign;
			}
			//mask the occupancy chunk for more precise skips
			//uvec4 prepos= uvec4(vec4(pos.xy,pos.z,pos.z));
			//uvec4 fullmask=premask<<prepos;
		}
		hasHit=GetOccupancyBit(chunk,pos);
		if(hasHit){
			break;
		}

		move= GetMove(itr);
		itr+= move*rayInv;
		pos+= move*raySign;
		if(debug_show_steps){steps+=move;}
	}

	if(hasHit){
		vec3 rpos = pos2*CHUNK_SIZE+pos;
		if (rpos == floor(camPos)){
			DEPTH = 0.;
			ALBEDO= vec3(0.);
		}else{
			vec3 lb= move*normalize(ray);
			float lambert= (lb.x+lb.y+lb.z)*0.3+0.7;
			vec3 col= GetColor(ivec3(rpos));
			if(debug_no_color){
				col= vec3(0.5);
			}
			if(!debug_no_light){
				col*= GetColor(ivec3(rpos-move*raySign));
			}
			ALBEDO = col*lambert;

			vec3 mini= (rpos-camPos + 1.-raySign01)*rayInv*raySign;
			float d  = max (mini.x, max (mini.y, mini.z));
			vec3 intersect = camPos + ray*d*raySign;
			vec4 yeet= PROJECTION_MATRIX*VIEW_MATRIX*vec4(intersect.xzy,1.);
			DEPTH=yeet.z/yeet.w;
		}
    }else{
		ALPHA=0.;
		DEPTH=0.;
	}
	if(debug_show_steps){
		float dis=1./float(render_distance+1u);
		steps *= dis;
		ALBEDO+= steps;
		ALPHA += (steps.x+steps.y+steps.z)*(dis*0.5+0.2);
	}
}