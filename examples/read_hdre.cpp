#include <iostream>
#include <fstream>
#include <cmath>
#include <cassert>
#include <algorithm>

#include "hdre.h"

void read__HDRE(const char* filename)
{
	// Create instance of HDRE
	HDRE * hdre = HDRE::Get(filename);

	if(!hdre) {
		std::cout << "can't load environment" << std::endl;
		return false;
	}

	// Set texture info from HDRE 
	unsigned int format = hdre->numChannels == 3 ? GL_RGB : GL_RGBA;
	unsigned int width = hdre->width;
	unsigned int height = hdre->height;
	float** data = hdre->getLevel(0).faces;

	// Create GPU texture to store environment info
	// This only stores the first mip (Specular reflection)
	GPUTexture * cube_texture = new GPUTexture(width, height, data, format, GL_FLOAT);

	/*
		The same for the rest of filtered levels.
		They can be saved in mipmap storage

		for (int i = 1; i < 6; i++)
		{
			sHDRELevel mip = hdre->getLevel(i);
			Texture * mip_texture = new Texture(mip.width, mip.height, mip.faces);
		}
	*/

	return true;
}