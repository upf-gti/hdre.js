
#define PI 3.14159265359
#define RECIPROCAL_PI 0.3183098861837697

varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;
varying vec4 v_color;

uniform vec3 u_camera_position;
uniform vec3 u_light_position;
uniform vec3 u_light_color;
uniform vec4 u_color;
uniform float u_exposure;

uniform sampler2D u_brdf;
uniform samplerCube u_texture;

uniform samplerCube u_texture_prem_0;
uniform samplerCube u_texture_prem_1;
uniform samplerCube u_texture_prem_2;
uniform samplerCube u_texture_prem_3;
uniform samplerCube u_texture_prem_4;

uniform sampler2D u_albedo_texture;
uniform sampler2D u_normal_texture;
uniform sampler2D u_metalness_texture;
uniform sampler2D u_roughness_texture;
uniform sampler2D u_emissive_texture;
uniform sampler2D u_ao_texture;
uniform sampler2D u_opacity_texture;


uniform float u_roughness;
uniform float u_metalness;
uniform float u_normal_scale;
uniform float u_light_intensity;

struct PBRMat
{
	float linearRoughness;
	float roughness;
	float metallic;
	float alpha;
	vec3 f0;
	vec3 reflectance;
	vec3 baseColor;
	vec3 diffuseColor;
	vec3 specularColor;
	vec3 reflection;
	vec3 N;
	vec3 V;
	vec3 H;
	float NoV;
	float NoL;
	float NoH;
	float LoH;
	float VoH;
	float clearCoat;
	float clearCoatRoughness;
	float clearCoatLinearRoughness;
};

vec3 getReflectionColor(vec3 r, float roughness)
{
	float lod = roughness * 5.0;

	vec4 color;

	if(lod < 1.0) color = mix( textureCube(u_texture, r), textureCube(u_texture_prem_0, r), lod );
	else if(lod < 2.0) color = mix( textureCube(u_texture_prem_0, r), textureCube(u_texture_prem_1, r), lod - 1.0 );
	else if(lod < 3.0) color = mix( textureCube(u_texture_prem_1, r), textureCube(u_texture_prem_2, r), lod - 2.0 );
	else if(lod < 4.0) color = mix( textureCube(u_texture_prem_2, r), textureCube(u_texture_prem_3, r), lod - 3.0 );
	else if(lod < 5.0) color = mix( textureCube(u_texture_prem_3, r), textureCube(u_texture_prem_4, r), lod - 4.0 );
	else color = textureCube(u_texture_prem_4, r);

	color = pow(color, vec4(1.0/2.2));

	return color.rgb;
}

//Javi Agenjo Snipet for Bump Mapping
mat3 cotangent_frame(vec3 N, vec3 p, vec2 uv){
	// get edge vectors of the pixel triangle
	vec3 dp1 = dFdx( p );
	vec3 dp2 = dFdy( p );
	vec2 duv1 = dFdx( uv );
	vec2 duv2 = dFdy( uv );

	// solve the linear system
	vec3 dp2perp = cross( dp2, N );
	vec3 dp1perp = cross( N, dp1 );
	vec3 T = dp2perp * duv1.x + dp1perp * duv2.x;
	vec3 B = dp2perp * duv1.y + dp1perp * duv2.y;

	// construct a scale-invariant frame
	float invmax = inversesqrt( max( dot(T,T), dot(B,B) ) );
	return mat3( T * invmax, B * invmax, N );
}

vec3 perturbNormal( vec3 N, vec3 V, vec2 texcoord, vec3 normal_pixel ){
	#ifdef USE_POINTS
	return N;
	#endif

	// assume N, the interpolated vertex normal and
	// V, the view vector (vertex to eye)
	//vec3 normal_pixel = texture2D(normalmap, texcoord ).xyz;
	normal_pixel = normal_pixel * 255./127. - 128./127.;
	mat3 TBN = cotangent_frame(N, V, texcoord);
	return normalize(TBN * normal_pixel);
}

void updateVectors (inout PBRMat material) {

	vec3 v = normalize(u_camera_position - v_world_position);
	vec3 n = normalize( v_normal );

	#ifdef USE_NORMAL_TEXTURE
		vec3 normal_map = texture2D(u_normal_texture, v_uv).xyz;
		n = mix(n, normalize( perturbNormal( v_normal, v, v_uv, normal_map ) ), u_normal_scale);
	#endif

	vec3 l = normalize(u_light_position - v_world_position);
	vec3 h = normalize(v + l);

	material.reflection = normalize(reflect(v, n));

	material.N = n;
	material.V = v;
	material.H = h;
	material.NoV = clamp(dot(n, v), 0.0, 0.99) + 1e-6;
	material.NoL = clamp(dot(n, l), 0.0, 0.99) + 1e-6;
	material.NoH = clamp(dot(n, h), 0.0, 0.99) + 1e-6;
	material.LoH = clamp(dot(l, h), 0.0, 0.99) + 1e-6;
	material.VoH = clamp(dot(v, h), 0.0, 0.99) + 1e-6;
}

void createMaterial (inout PBRMat material) {
		
	float metallic = u_metalness;//max(0.02, u_metalness);
	#ifdef USE_METALLIC_TEXTURE
		metallic *= texture2D(u_metalness_texture, v_uv).r;
	#endif
	
	vec3 baseColor = u_color;
	#ifdef USE_ALBEDO_TEXTURE
		baseColor *= texture2D(u_albedo_texture, v_uv).rgb;
	#endif

	// GET COMMON MATERIAL PARAMS
	vec3 reflectance =  0.5; // DEFAULT FOR MOST MATERIALS
	vec3 diffuseColor = (1.0 - metallic) * baseColor;
	vec3 f0 = mix(vec3(0.04), baseColor, metallic);

	// GET ROUGHNESS PARAMS
	float roughness = u_roughness;
	#ifdef USE_ROUGHNESS_TEXTURE
		vec4 sampler = texture2D(u_roughness_texture, v_uv);
		if(true) // metallic-roughness map
		{
			roughness *= sampler.g;
			metallic = u_metalness * sampler.b; // recompute metalness
			diffuseColor = (1.0 - metallic) * baseColor; // recompute metallic
		}
		else
			roughness *= sampler.r;
	#endif

	roughness = max(roughness, 0.04);
	roughness = min(roughness, 0.99);

	float linearRoughness = roughness * roughness;

	material.roughness = roughness;
	material.linearRoughness = linearRoughness;
	material.metallic = metallic;
	material.f0 = f0;
	material.diffuseColor = diffuseColor;
	material.baseColor = baseColor;
	material.reflectance = reflectance;
	
	updateVectors( material );
}

void ibl (PBRMat material, inout vec3 Fd, inout vec3 Fr) {
	
	float NdotV = material.NoV;

	vec2 brdfSamplePoint = vec2(NdotV, material.roughness);
	vec2 brdf = texture2D(u_brdf, brdfSamplePoint).rg;

	vec3 diffuseSample = getReflectionColor(-material.N, 1.0);
	vec3 specularSample = getReflectionColor(material.reflection, material.roughness);

	vec3 specularColor = mix(material.f0, material.baseColor.rgb, material.metallic);

	Fd += diffuseSample * material.diffuseColor;
	Fr += specularSample * (specularColor * brdf.x + brdf.y);
}

// Normal Distribution Function (NDC) using GGX Distribution
float D_GGX (const in float NoH, const in float linearRoughness ) {
	float a2 = linearRoughness * linearRoughness;
	float f = (NoH * a2 - NoH) * NoH + 1.0;
	return a2 / (PI * f * f);
}

// Geometry Term : Geometry masking / shadowing due to microfacets
float GGX(float NdotV, float k){
	return NdotV / (NdotV * (1.0 - k) + k);
}
	
float G_Smith(float NdotV, float NdotL, float linearRoughness){
	float k = linearRoughness / 2.0;
	return GGX(NdotL, k) * GGX(NdotV, k);
}

// Fresnel term with scalar optimization(f90=1)
vec3 F_Schlick (const in float VoH, const in vec3 f0) {
	float f = pow(1.0 - VoH, 5.0);
	return f0 + (vec3(1.0) - f0) * f;
}

// Diffuse Reflections: Disney BRDF using retro-reflections using F term
float Fd_Burley (const in float NoV, const in float NoL, const in float LoH, const in float linearRoughness) {
	float f90 = 0.5 + 2.0 * linearRoughness * LoH * LoH;
	float lightScatter = 1.0 + (f90 - 1.0) * pow(1.0 - NoL, 5.0);
	float viewScatter  = 1.0 + (f90 - 1.0) * pow(1.0 - NoV, 5.0);
	return lightScatter * viewScatter * RECIPROCAL_PI;
}

vec3 specularBRDF( const in PBRMat material ) {

	// Normal Distribution Function
	float D = D_GGX( material.NoH, material.linearRoughness );
	// Visibility Function (shadowing/masking)
	float V = G_Smith( material.NoV, material.NoL, material.linearRoughness );
	// Fresnel
	vec3 F = F_Schlick( material.LoH, material.f0 );

	vec3 spec = (D * V) * F;
	spec /= (4.0 * material.NoL * material.NoV + 1e-6);

	return spec;
}

void do_lighting(inout PBRMat material, inout vec3 color)
{
	// INDIRECT LIGHT: IBL ********************
	vec3 Fd_i = vec3(0.0);
	vec3 Fr_i = vec3(0.0);
	ibl(material, Fd_i, Fr_i);

	vec3 indirect = Fd_i + Fr_i;

	// Apply ambient oclusion 
	#ifdef USE_OCCLUSION_TEXTURE
		indirect *= texture2D(u_ao_texture, v_uv).r;
	#endif
		
	// DIRECT LIGHT ***************************

	vec3 Fs_d = specularBRDF( material );
	vec3 Fd_d = material.diffuseColor * Fd_Burley (material.NoV, material.NoL, material.LoH, material.linearRoughness);
	vec3 direct = Fs_d + Fd_d;

	// COMPOSE

	vec3 lightParams = material.NoL * u_light_color * u_light_intensity;
	color  = indirect;
	color += direct * lightParams;
}

const float GAMMA = 2.2;
const float INV_GAMMA = 1.0 / GAMMA;

// Uncharted 2 tone map
// see: http://filmicworlds.com/blog/filmic-tonemapping-operators/
vec3 toneMapUncharted2Impl(vec3 color)
{
    const float A = 0.15;
    const float B = 0.50;
    const float C = 0.10;
    const float D = 0.20;
    const float E = 0.02;
    const float F = 0.30;
    return ((color*(A*color+C*B)+D*E)/(color*(A*color+B)+D*F))-E/F;
}

vec3 toneMapUncharted(vec3 color)
{
    const float W = 11.2;
    color = toneMapUncharted2Impl(color * 2.0);
    vec3 whiteScale = 1.0 / toneMapUncharted2Impl(vec3(W));
    return color * whiteScale;
}

void main()
{
	PBRMat material;
	vec3 color;
	float alpha = 1.0;

	createMaterial( material );
	do_lighting( material, color);

	#ifdef USE_EMISSION_TEXTURE
		color += texture2D(u_emissive_texture, v_uv).rgb;
	#endif

	#ifdef USE_ALPHA_TEXTURE
		alpha = texture2D(u_opacity_texture, v_uv).r;
	#endif
       
	// Tonemapping here is not a good option since accumulated
	// renderings would not be shown correctly.
	color = toneMapUncharted(color) * u_exposure;

	gl_FragColor = vec4(color, alpha);
}