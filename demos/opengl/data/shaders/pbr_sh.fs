
varying vec3 v_position;
varying vec3 v_world_position;
varying vec3 v_normal;
varying vec2 v_uv;

uniform vec3 u_sh_coeffs[9];

const float Pi = 3.141592654;
const float CosineA0 = Pi;
const float CosineA1 = (2.0 * Pi) / 3.0;
const float CosineA2 = Pi * 0.25;

struct SH9
{
    float c[9];
};

struct SH9Color
{
    vec3 c[9];
};

void SHCosineLobe(in vec3 dir, out SH9 sh)
{
	
    // Band 0
    sh.c[0] = 0.282095 * CosineA0;
	
    // Band 1
    sh.c[1] = 0.488603 * dir.y * CosineA1;
    sh.c[2] = 0.488603 * dir.z * CosineA1;
    sh.c[3] = 0.488603 * dir.x * CosineA1;
	
    // Band 2
	#ifndef SH_LOW
	
    sh.c[4] = 1.092548 * dir.x * dir.y * CosineA2;
    sh.c[5] = 1.092548 * dir.y * dir.z * CosineA2;
    sh.c[6] = 0.315392 * (3.0 * dir.z * dir.z - 1.0) * CosineA2;
    sh.c[7] = 1.092548 * dir.x * dir.z * CosineA2;
    sh.c[8] = 0.546274 * (dir.x * dir.x - dir.y * dir.y) * CosineA2;
	#endif
	
}

vec3 ComputeSHIrradiance(in vec3 normal, in SH9Color radiance)
{
	normal.y = -normal.y;

    // Compute the cosine lobe in SH, oriented about the normal direction
    SH9 shCosine;
	SHCosineLobe(normal, shCosine);

    // Compute the SH dot product to get irradiance
    vec3 irradiance = vec3(0.0);
	#ifndef SH_LOW
	const int num = 9;
	#else
	const int num = 4;
	#endif
	
    for(int i = 0; i < num; ++i)
        irradiance += radiance.c[i] * shCosine.c[i];
	
    return irradiance;
}

vec3 ComputeSHDiffuse(in vec3 normal, in SH9Color radiance)
{
    // Diffuse BRDF is albedo / Pi
    return ComputeSHIrradiance( normal, radiance ) * (1.0 / Pi);
}

void main()
{
	vec3 normal = normalize( v_normal );
	SH9Color coeffs;
	coeffs.c[0] = u_sh_coeffs[0];
	coeffs.c[1] = u_sh_coeffs[1];
	coeffs.c[2] = u_sh_coeffs[2];
	coeffs.c[3] = u_sh_coeffs[3];
	coeffs.c[4] = u_sh_coeffs[4];
	coeffs.c[5] = u_sh_coeffs[5];
	coeffs.c[6] = u_sh_coeffs[6];
	coeffs.c[7] = u_sh_coeffs[7];
	coeffs.c[8] = u_sh_coeffs[8];

	vec3 irradiance = ComputeSHDiffuse( normal, coeffs );

	gl_FragColor =  vec4(max( vec3(0.001), irradiance ), 1.0 );
}