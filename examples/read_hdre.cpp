#include <iostream>
#include <fstream>
#include <cmath>
#include <cassert>
#include <algorithm>

#include "hdre.h"

void read__HDRE()
{
	// Create instance of HDRE
	HDRE * hdre = new HDRE("filename.hdre");

	// Get data from file
	unsigned int width = hdre->width;
	unsigned int height = hdre->height;
	float** data = hdre->getLevel(0).faces;

	// Create GPU texture to store environment info
	// This only stores the first mip (Specular reflection)
	Texture * cube_texture = new Texture(width, height, data);

	/*
		The same for the rest of filtered levels.

		for (int i = 1; i < 6; i++)
		{
			sHDRELevel mip = hdre->getLevel(i);
			Texture * mip_texture = new Texture(mip.width, mip.height, mip.faces);
		}
	*/

	// Create skybox node and set cubemap texture
	Skybox* skybox = new Skybox(cube_texture);
}