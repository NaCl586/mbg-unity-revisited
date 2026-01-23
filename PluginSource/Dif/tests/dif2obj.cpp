//------------------------------------------------------------------------------
// Copyright (c) 2015 HiGuy Smith
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
//  * Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
//  * Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
//  * Neither the name of the project nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
// ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//------------------------------------------------------------------------------

#include <iostream>
#include <sstream>
#include <fstream>
#include <dif/objects/dif.h>

void exportObj(const DIF::Interior &dif, const std::string &outFile) {
	struct Face {
		DIF::U32 vertIndex[3];
		DIF::U32 texCoordIndex[3];
		DIF::U32 normalIndex;
	};

	std::vector<glm::vec3> vertices;
	std::vector<glm::vec2> texCoords;
	std::vector<glm::vec3> normals;
	std::map<DIF::U32, std::vector<Face>> faces;

	vertices.insert(vertices.end(), dif.point.begin(), dif.point.end());
	normals.insert(normals.end(), dif.normal.begin(), dif.normal.end());

	for (const DIF::Interior::Surface &surface : dif.surface) {
		//New and improved rendering with actual Triangle Strips this time
		for (DIF::U32 j = surface.windingStart + 2; j < surface.windingStart + surface.windingCount; j ++) {
			Face f;

			if ((j - (surface.windingStart + 2)) % 2 == 0) {
				f.vertIndex[0] = dif.index[j];
				f.vertIndex[1] = dif.index[j - 1];
				f.vertIndex[2] = dif.index[j - 2];
			} else {
				f.vertIndex[0] = dif.index[j - 2];
				f.vertIndex[1] = dif.index[j - 1];
				f.vertIndex[2] = dif.index[j];
			}

			f.normalIndex = dif.plane[surface.planeIndex].normalIndex;
			if (surface.planeFlipped) {
				f.normalIndex += normals.size();
			}

			DIF::Interior::TexGenEq texGen = dif.texGenEq[surface.texGenIndex];

			const glm::vec3 &pt0 = dif.point[f.vertIndex[0]];
			const glm::vec3 &pt1 = dif.point[f.vertIndex[1]];
			const glm::vec3 &pt2 = dif.point[f.vertIndex[2]];

			glm::vec2 coord0(pt0.x * texGen.planeX.x + pt0.y * texGen.planeX.y + pt0.z * texGen.planeX.z + texGen.planeX.d,
								pt0.x * texGen.planeY.x + pt0.y * texGen.planeY.y + pt0.z * texGen.planeY.z + texGen.planeY.d);
			glm::vec2 coord1(pt1.x * texGen.planeX.x + pt1.y * texGen.planeX.y + pt1.z * texGen.planeX.z + texGen.planeX.d,
								pt1.x * texGen.planeY.x + pt1.y * texGen.planeY.y + pt1.z * texGen.planeY.z + texGen.planeY.d);
			glm::vec2 coord2(pt2.x * texGen.planeX.x + pt2.y * texGen.planeX.y + pt2.z * texGen.planeX.z + texGen.planeX.d,
								pt2.x * texGen.planeY.x + pt2.y * texGen.planeY.y + pt2.z * texGen.planeY.z + texGen.planeY.d);

			f.texCoordIndex[0] = texCoords.size();
			texCoords.push_back(coord0);
			f.texCoordIndex[1] = texCoords.size();
			texCoords.push_back(coord1);
			f.texCoordIndex[2] = texCoords.size();
			texCoords.push_back(coord2);

			faces[surface.textureIndex].push_back(f);
		}
	}

	//Obj file
	std::ofstream out(outFile);

	//Vertex list
	for (const glm::vec3 &point : vertices) {
		out << "v " << -point.x << " " << point.z << " " << point.y << "\n";
	}

	//Texture coords
	for (const glm::vec2 &texCoord : texCoords) {
		out << "vt " << texCoord.x << " " << -texCoord.y << "\n";
	}

	//Normals
	for (const glm::vec3 &normal : normals) {
		out << "vn " << -normal.x << " " << normal.z << " " << normal.y << "\n";
	}
	//Because torque likes to flip normals
	for (const glm::vec3 &normal : normals) {
		out << "vn " << normal.x << " " << -normal.z << " " << -normal.y << "\n";
	}

	//Faces
	for (const auto &pair : faces) {
		out << "g ";
		out << dif.materialName[pair.first] << "\n";
		for (const Face &face : pair.second) {
			out << "f";
			out << " " << (face.vertIndex[0] + 1) << "/" << (face.texCoordIndex[0] + 1) << "/" << (face.normalIndex + 1);
			out << " " << (face.vertIndex[1] + 1) << "/" << (face.texCoordIndex[1] + 1) << "/" << (face.normalIndex + 1);
			out << " " << (face.vertIndex[2] + 1) << "/" << (face.texCoordIndex[2] + 1) << "/" << (face.normalIndex + 1);
			out << "\n";
		}
	}

	out.close();
}

bool readDif(const std::string &file, DIF::DIF &dif, DIF::Version &version) {
	std::ifstream stream(file, std::ios::binary);
	if (stream.good()) {
		bool success = dif.read(stream, version);
		stream.close();
		if (!success) {
			fprintf(stderr, "Error parsing dif!\n");
		}
		return success;
	} else {
		fprintf(stderr, "Could not open file %s!\n", file.c_str());
	}
	return false;
}

int main(int argc, const char * argv[]) {
	if (argc < 2) {
		fprintf(stderr, "Usage: %s <interior0.dif> [interior1.dif ...]\n", argv[0]);
		return EXIT_FAILURE;
	}

	for (int i = 1; i < argc; i ++) {
		std::string difPath = argv[i];
		std::string objPath;
		size_t dot = difPath.find_last_of('.');

		if (dot == std::string::npos) {
			objPath = difPath + ".obj";
		} else {
			objPath = difPath.substr(0, dot) + ".obj";
		}

		DIF::DIF dif;
		DIF::Version inVersion;
		if (readDif(difPath, dif, inVersion)) {
			exportObj(dif.interior[0], objPath);
		} else {
			return EXIT_FAILURE;
		}
	}
	return EXIT_SUCCESS;
}
