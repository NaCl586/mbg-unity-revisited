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

std::string getPolyhedron(const DIF::Trigger &trigger) {
	// First point is corner, need to find the three vectors...
	glm::vec3 origin = trigger.polyhedron.pointList[0];
	DIF::U32 currVec = 0;
	glm::vec3 vecs[3];
	for (DIF::U32 i = 0; i < trigger.polyhedron.edgeList.size(); i++) {
		const DIF::U32 *vertex = trigger.polyhedron.edgeList[i].vertex;
		if (vertex[0] == 0)
			vecs[currVec++] = trigger.polyhedron.pointList[vertex[1]] - origin;
		else
			if (vertex[1] == 0)
				vecs[currVec++] = trigger.polyhedron.pointList[vertex[0]] - origin;
	}

	// Build output string.
	std::stringstream ss;
	ss << origin.x << ' ' << origin.y << ' ' << origin.z << ' ' <<
	vecs[0].x << ' ' << vecs[0].y << ' ' << vecs[0].z << ' ' <<
	vecs[1].x << ' ' << vecs[1].y << ' ' << vecs[1].z << ' ' <<
	vecs[2].x << ' ' << vecs[2].y << ' ' << vecs[2].z;
	return ss.str();
}

void printTriggers(const DIF::DIF &dif) {
	for (const DIF::Trigger &trigger : dif.trigger) {
		std::cout << "new Trigger(" << trigger.name << ") {" << std::endl;
		std::cout << "   position = \"" << trigger.offset.x << " " << trigger.offset.y << " " << trigger.offset.z << "\";" << std::endl;
		std::cout << "   rotation = \"1 0 0 0\";" << std::endl;
		std::cout << "   scale = \"1 1 1 \";" << std::endl;
		std::cout << "   datablock = \"" << trigger.datablock <<  "\";" << std::endl;
		std::cout << "   polyhedron = \"" << getPolyhedron(trigger) << "\";" << std::endl;
		for (const auto &it : trigger.properties) {
			std::cout << "      " << it.first << " = \"" << it.second << "\";" << std::endl;
		}
		std::cout << "};" << std::endl;
	}
}

void printEntities(const DIF::DIF &dif) {
	for (const DIF::GameEntity &entity : dif.gameEntity) {
		std::cout << "new " << entity.gameClass << "() {" << std::endl;
		std::cout << "   position = \"" << entity.position.x << " " << entity.position.y << " " << entity.position.z << "\";" << std::endl;
		std::cout << "   rotation = \"1 0 0 0\";" << std::endl;
		std::cout << "   scale = \"1 1 1 \";" << std::endl;
		std::cout << "   datablock = \"" << entity.datablock <<  "\";" << std::endl;
		for (const auto &it : entity.properties) {
			std::cout << "      " << it.first << " = \"" << it.second << "\";" << std::endl;
		}
		std::cout << "};" << std::endl;
	}
}

void nullSurfaces(DIF::DIF &dif) {
	for (DIF::Interior &interior : dif.interior) {
		for (DIF::Interior::Surface &surface : interior.surface) {
			//Get average position
			glm::vec3 position;

			for (DIF::U32 i = 0; i < surface.windingCount; i ++) {
				DIF::U32 index = i + surface.windingStart;
				glm::vec3 point = interior.point[interior.index[index]];
				position += point;
			}

			position /= surface.windingCount;
			if (position.z > 50) {
				surface.textureIndex = 0;
			}

			std::cout << "Average position for surface: " << position.x << ", " << position.y << ", " << position.z << std::endl;
		}
	}
}

void exportObj(const DIF::Interior &dif, const char *outFile) {
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

void exportJSON(const DIF::Interior &dif, const char *outFile) {
	struct Face {
		DIF::U32 vertIndex[3];
		DIF::U32 texCoordIndex[3];
		DIF::U32 normalIndex;
		DIF::U32 tangentIndex;
		DIF::U32 bitangentIndex;
	};

	struct TextureGroup {
		std::vector<Face> faces;
	};

	std::vector<glm::vec3> vertices;
	std::vector<glm::vec2> texCoords;
	std::vector<glm::vec3> normals;
	std::vector<glm::vec3> tangents;
	std::vector<glm::vec3> bitangents;

	std::map<DIF::U16, TextureGroup> texGroups;

	vertices.insert(vertices.end(), dif.point.begin(), dif.point.end());
	normals.insert(normals.end(), dif.normal.begin(), dif.normal.end());

	for (const DIF::Interior::Surface &surface : dif.surface) {
		glm::vec3 normal = dif.normal[dif.plane[surface.planeIndex].normalIndex];
		if (surface.planeFlipped) {
			normal *= -1;
		}
		normals.push_back(normal);

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

			if (surface.planeFlipped) {
				f.normalIndex = normals.size() - 1;
			} else {
				f.normalIndex = dif.plane[surface.planeIndex].normalIndex;
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

			glm::vec3 deltaPos1 = pt1 - pt0;
			glm::vec3 deltaPos2 = pt2 - pt0;
			glm::vec2 deltaUV1 = coord1 - coord0;
			glm::vec2 deltaUV2 = coord2 - coord0;

			DIF::F32 r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);

			glm::vec3 tangent   = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;
			glm::vec3 bitangent = (deltaPos2 * deltaUV1.x - deltaPos1 * deltaUV2.x) * r;

			tangent = glm::normalize(tangent - (normal * glm::dot(normal, tangent)));
			if (glm::dot(glm::cross(normal, tangent), bitangent) < 0.0f) {
				tangent *= -1.0f;
			}

			tangent = glm::normalize(tangent);
			bitangent = glm::normalize(bitangent);

			f.tangentIndex = tangents.size();
			tangents.push_back(tangent);
			f.bitangentIndex = bitangents.size();
			bitangents.push_back(bitangent);

			texGroups[surface.textureIndex].faces.push_back(f);
		}
	}

#define NEWLINE "\n"
//#define NEWLINE ""

	//Obj file
	std::ofstream out(outFile);

	out << "nan=0;model={" NEWLINE;
	out << "textures:[" NEWLINE;

	DIF::U32 start = 0;
	for (DIF::U32 i = 0; i < dif.materialName.size(); i ++) {
		if (i > 0)
			out << "," NEWLINE;

		out << "{texture:\"";
		out << dif.materialName[i];
		out << "\",start:";
		out << start;
		out << ",count:";
		out << texGroups[i].faces.size();
		out << "}";

		start += texGroups[i].faces.size();
	}

	out << "]," NEWLINE;
	out << "faces:[" NEWLINE;

	for (DIF::U32 i = 0; i < dif.materialName.size(); i ++) {
		const TextureGroup &group = texGroups[i];

		if (i > 0 && texGroups[i - 1].faces.size() > 0)
			out << "," NEWLINE;

		for (DIF::U32 j = 0; j < group.faces.size(); j ++) {
			const Face &face = group.faces[j];

			const glm::vec3 &point0    = vertices[face.vertIndex[0]];
			const glm::vec3 &point1    = vertices[face.vertIndex[1]];
			const glm::vec3 &point2    = vertices[face.vertIndex[2]];
			const glm::vec2 &texCoord0 = texCoords[face.texCoordIndex[0]];
			const glm::vec2 &texCoord1 = texCoords[face.texCoordIndex[1]];
			const glm::vec2 &texCoord2 = texCoords[face.texCoordIndex[2]];
			const glm::vec3 &normal    = normals[face.normalIndex];
			const glm::vec3 &tangent   = tangents[face.tangentIndex];
			const glm::vec3 &bitangent = bitangents[face.bitangentIndex];

			if (j > 0)
				out << "," NEWLINE;
			out << point0.x << "," << point0.y << "," << point0.z << ",";
			out << texCoord0.x << "," << texCoord0.y << ",";
			out << normal.x << "," << normal.y << "," << normal.z << ",";
			out << tangent.x << "," << tangent.y << "," << tangent.z << ",";
			out << bitangent.x << "," << bitangent.y << "," << bitangent.z << "," NEWLINE;

			out << point1.x << "," << point1.y << "," << point1.z << ",";
			out << texCoord1.x << "," << texCoord1.y << ",";
			out << normal.x << "," << normal.y << "," << normal.z << ",";
			out << tangent.x << "," << tangent.y << "," << tangent.z << ",";
			out << bitangent.x << "," << bitangent.y << "," << bitangent.z << "," NEWLINE;

			out << point2.x << "," << point2.y << "," << point2.z << ",";
			out << texCoord2.x << "," << texCoord2.y << ",";
			out << normal.x << "," << normal.y << "," << normal.z << ",";
			out << tangent.x << "," << tangent.y << "," << tangent.z << ",";
			out << bitangent.x << "," << bitangent.y << "," << bitangent.z;
		}
	}

	out << "]" NEWLINE;
	out << "}" NEWLINE;
	
	out.close();
}

DIF::PlaneF getPlane(glm::vec3 Point1, glm::vec3 Point2, glm::vec3 Point3) {
	glm::vec3 normal;
	DIF::F64 dist;

	normal.x = Point1.y * Point2.z - Point1.y * Point3.z +
	Point3.y * Point1.z - Point2.y * Point1.z +
	Point2.y * Point3.z - Point3.y * Point2.z;
	normal.y = Point1.x * Point2.z - Point1.x * Point3.z +
	Point3.x * Point1.z - Point2.x * Point1.z +
	Point2.x * Point3.z - Point3.x * Point2.z;
	normal.z = Point1.x * Point2.y - Point1.x * Point3.y +
	Point3.x * Point1.y - Point2.x * Point1.y +
	Point2.x * Point3.y - Point3.x * Point2.y;
	dist     = Point1.x * Point2.y * Point3.z - Point1.x * Point2.z * Point3.y +
	Point1.y * Point2.z * Point3.x - Point1.y * Point2.x * Point3.z +
	Point1.z * Point2.x * Point3.y - Point1.z * Point2.y * Point3.x;

	normal.x = -normal.x;
	normal.z = -normal.z;

	return DIF::PlaneF(normal.x, normal.y, normal.z, dist);
}

void findPolys(DIF::DIF &dif) {
	DIF::F32 scale = 32.0f;
	for (DIF::Interior &interior : dif.interior) {
		std::cout << "//" << std::endl;
		std::cout << "//" << std::endl;
		std::cout << "" << std::endl;
		std::cout << "{" << std::endl;
		std::cout << "   \"classname\" \"worldspawn\"" << std::endl;
		std::cout << "   \"detail_number\" \"0\"" << std::endl;
		std::cout << "   \"min_pixels\" \"250\"" << std::endl;
		std::cout << "   \"geometry_scale\" \"" << scale << "\"" << std::endl;
		std::cout << "   \"light_geometry_scale\" \"" << (scale / 4) << "\"" << std::endl;
		std::cout << "   \"ambient_color\" \"0 0 0\"" << std::endl;
		std::cout << "   \"emergency_ambient_color\" \"0 0 0\"" << std::endl;
		std::cout << "   \"mapversion\" \"220\"" << std::endl;

		for (DIF::U32 hullNum = 0; hullNum < interior.convexHull.size(); hullNum ++) {
			const DIF::Interior::ConvexHull &hull = interior.convexHull[hullNum];
			std::cout << "" << std::endl;
			std::cout << "   // Brush " << hullNum << std::endl;
			std::cout << "   {" << std::endl;
			//Get hull planes

			//DIF::U32 numVerts = interior.convexHullEmitStringCharacter[hull.polyListStringStart];
			//DIF::U32 numEdges = interior.convexHullEmitStringCharacter[hull.polyListStringStart + numVerts];
			//DIF::U32 numFaces = interior.convexHullEmitStringCharacter[hull.polyListStringStart + numVerts + (numEdges * 2)];

			for (DIF::U32 i = hull.surfaceStart; i < hull.surfaceStart + hull.surfaceCount; i ++) {
				DIF::Interior::Surface surf = interior.surface[interior.hullSurfaceIndex[i]];
				DIF::Interior::Plane plane = interior.plane[surf.planeIndex];
				glm::vec3 planeNormal = interior.normal[plane.normalIndex];
				if (surf.planeFlipped) {
					planeNormal *= -1.0f;
					plane.planeDistance *= -1;
				}
				std::string texture = interior.materialName[surf.textureIndex];
				DIF::Interior::TexGenEq texGen = interior.texGenEq[surf.texGenIndex];

				glm::vec3 planeX = glm::vec3(texGen.planeX.x, texGen.planeX.y, texGen.planeX.z);
				texGen.planeX.d *= scale / glm::length(planeX);
				planeX = glm::normalize(planeX);

				glm::vec3 planeY = glm::vec3(texGen.planeY.x, texGen.planeY.y, texGen.planeY.z);
				texGen.planeY.d *= scale / glm::length(planeY);
				planeY = glm::normalize(planeY);

				//Find 3 verts in the hull that match up to this plane
				std::vector<glm::vec3> points;
				for (DIF::U32 j = hull.hullStart; j < hull.hullStart + hull.hullCount; j ++) {
					glm::vec3 point = interior.point[interior.hullIndex[j]];

					if (glm::dot(point, planeNormal) + plane.planeDistance == 0) {
						points.push_back(point * scale);
						if (points.size() == 3)
							break;
					}
				}

				assert(points.size() == 3);

				std::cout << "      ";
				glm::vec3 nplane = glm::normalize(glm::cross(points[2] - points[1], points[2] - points[0]));
				if (nplane == -planeNormal) {
					for (DIF::S32 j = 2; j >= 0; j --) {
						std::cout << "( " << points[j].x << " " << points[j].y << " " << points[j].z << " ) ";
					}
				} else {
					for (DIF::U32 j = 0; j < 3; j ++) {
						std::cout << "( " << points[j].x << " " << points[j].y << " " << points[j].z << " ) ";
					}
				}


				//Then the texture name
				std::cout << texture << " ";

				//Then the texgen
				std::cout << "[ " << planeX.x << " " << planeX.y << " " << planeX.z << " " << texGen.planeX.d << " ] "
				          << "[ " << planeY.x << " " << planeY.y << " " << planeY.z << " " << texGen.planeY.d << " ] ";
				std::cout << "0 1 1" << std::endl;
			}
			std::cout << "   }" << std::endl;
		}
		std::cout << "}" << std::endl;
	}
}

bool readDif(const char *file, DIF::DIF &dif, DIF::Version &version) {
	std::ifstream stream(file, std::ios::binary);
	if (stream.good()) {
		bool success = dif.read(stream, version);
		stream.close();
		return success;
	}
	return false;
}

bool testEquality(const char *file) {
	DIF::DIF dif;
	DIF::Version inVersion;
	DIF::Version outVersion(DIF::Version::DIFVersion(44), DIF::Version::InteriorVersion(0, DIF::Version::InteriorVersion::Type::MBG), DIF::Version::MaterialListVersion(1), DIF::Version::VehicleCollisionFileVersion(0));
	//Make sure we can actually read/write the dif first
	if (readDif(file, dif, inVersion)) {
		std::ostringstream out;
		if (dif.write(out, outVersion)) {
			out.flush();

			//Take the written output of the DIF and feed it back into itself
			std::stringstream in;
			in << out.str();

			//Clear the output stream so we can reuse it
			out.clear();
			if (dif.read(in, inVersion) && dif.write(out, outVersion)) {
				//Read the two strings from their streams
				std::string fileStr = in.str();
				std::string difStr = out.str();

				//Check the two strings for equality
				for (auto i = 0; i < fileStr.size(); i ++) {
					if (fileStr[i] != difStr[i]) {
						//Inconsistency
						std::cout << "DIF output inconsistency starting at offset " << i << " (0x" << std::hex << i << std::dec << ")" << std::endl;
						return false;
					}
				}

				//No error? They're the same
				return true;
			}
		}
	}
	//Couldn't read/write somewhere along the lines
	return false;
}

bool convertDif(const char *file) {
	DIF::DIF dif;
	DIF::Version inVersion;
	DIF::Version outVersion(DIF::Version::DIFVersion(44), DIF::Version::InteriorVersion(0, DIF::Version::InteriorVersion::Type::MBG), DIF::Version::MaterialListVersion(1), DIF::Version::VehicleCollisionFileVersion(0));
	if (readDif(file, dif, inVersion)) {
		//Save it again
		std::ofstream out(file, std::ios::binary);
		return dif.write(out, outVersion);
	}
	return false;
}

bool scaleTexture(DIF::DIF &dif, const std::string &textureName, const glm::vec2 &scale) {
	for (DIF::Interior &interior : dif.interior) {
		std::vector<DIF::U32> texGensToChange;

		for (DIF::Interior::Surface &surface : interior.surface) {
			if (interior.materialName[surface.textureIndex] != textureName)
				continue;

			if (std::find(texGensToChange.begin(), texGensToChange.end(), surface.texGenIndex) == texGensToChange.end()) {
				texGensToChange.push_back(surface.texGenIndex);
			}
		}

		for (DIF::U32 index : texGensToChange) {
			DIF::Interior::TexGenEq &texGen = interior.texGenEq[index];
			texGen.planeX.x *= scale.x;
			texGen.planeX.y *= scale.x;
			texGen.planeX.z *= scale.x;
			texGen.planeX.d *= scale.x;
			texGen.planeY.x *= scale.y;
			texGen.planeY.y *= scale.y;
			texGen.planeY.z *= scale.y;
			texGen.planeX.d *= scale.x;
		}
	}

	return true;
}

void printTextures(DIF::DIF &dif) {
	for (DIF::Interior &interior : dif.interior) {
		for (const std::string &name : interior.materialName) {
			printf("%s\n", name.c_str());
		}
	}
	for (DIF::Interior &interior : dif.subObject) {
		for (const std::string &name : interior.materialName) {
			printf("%s\n", name.c_str());
		}
	}
}

int main(int argc, const char * argv[]) {
	if (argc > 2 && strcmp(argv[1], "--null") == 0) {
		DIF::DIF dif;
		DIF::Version inVersion;
		DIF::Version outVersion(DIF::Version::DIFVersion(44), DIF::Version::InteriorVersion(0, DIF::Version::InteriorVersion::Type::MBG), DIF::Version::MaterialListVersion(1), DIF::Version::VehicleCollisionFileVersion(0));
		if (readDif(argv[2], dif, inVersion)) {
			nullSurfaces(dif);
			std::ofstream out(argv[3], std::ios::binary);
			return dif.write(out, outVersion) ? EXIT_SUCCESS : EXIT_FAILURE;
		}
	}
	if (argc > 2 && strcmp(argv[1], "--convert") == 0) {
		//Convert the DIF
		return convertDif(argv[2]) ? EXIT_SUCCESS : EXIT_FAILURE;
	}
	if (argc > 3 && strcmp(argv[1], "--export") == 0) {
		DIF::DIF dif;
		DIF::Version inVersion;
		if (readDif(argv[2], dif, inVersion)) {
			exportObj(dif.interior[0], argv[3]);
		}
		return EXIT_SUCCESS;
	}
	if (argc > 3 && strcmp(argv[1], "--json") == 0) {
		DIF::DIF dif;
		DIF::Version inVersion;
		if (readDif(argv[2], dif, inVersion)) {
			exportJSON(dif.interior[0], argv[3]);
		}
		return EXIT_SUCCESS;
	}
	if (argc > 5 && strcmp(argv[1], "--scale") == 0) {
		std::string textureName = argv[2];
		glm::vec2 scale(atof(argv[3]), atof(argv[4]));
		DIF::DIF dif;
		DIF::Version inVersion;
		DIF::Version outVersion(DIF::Version::DIFVersion(44), DIF::Version::InteriorVersion(0, DIF::Version::InteriorVersion::Type::MBG), DIF::Version::MaterialListVersion(1), DIF::Version::VehicleCollisionFileVersion(0));
		if (readDif(argv[5], dif, inVersion)) {
			scaleTexture(dif, textureName, scale);
			std::ofstream out(argv[5], std::ios::binary);
			return dif.write(out, outVersion) ? EXIT_SUCCESS : EXIT_FAILURE;
		}
		return EXIT_FAILURE;
	}
	if (argc > 2 && strcmp(argv[1], "--textures") == 0) {
		for (int i = 2; i < argc; i ++) {
			DIF::DIF dif;
			DIF::Version inVersion;
			if (readDif(argv[i], dif, inVersion)) {
				printTextures(dif);
			} else {
				return EXIT_FAILURE;
			}
		}
		return EXIT_SUCCESS;
	}
	if (argc > 2 && strcmp(argv[1], "--polys") == 0) {
		for (int i = 2; i < argc; i ++) {
			DIF::DIF dif;
			DIF::Version inVersion;
			if (readDif(argv[i], dif, inVersion)) {
				findPolys(dif);
			}
		}
		return EXIT_SUCCESS;
	}

	for (int i = 1; i < argc; i ++) {
		//Read it into the dif
		DIF::DIF dif;
		DIF::Version inVersion;
		if (readDif(argv[i], dif, inVersion)) {
			std::cout << "Dif information for " << argv[i] << std::endl;
			std::cout << "   DIF File Version: " << inVersion.dif.to_string() << std::endl;
			std::cout << "      Interior Version " << inVersion.interior.to_string() << std::endl;
			std::cout << "      Material Version " << inVersion.material.to_string() << std::endl;
			std::cout << "      Vehicle Collision Version " << inVersion.vehicleCollision.to_string() << std::endl;

			std::cout << "   Interior Count: " << std::to_string(dif.interior.size()) << std::endl;

			for (DIF::U32 i = 0; i < dif.interior.size(); i ++) {
				const DIF::Interior &interior = dif.interior[i];
				std::cout << "      Interior " << std::to_string(i) << std::endl;
				std::cout << "      At offset 0x" << std::hex << std::uppercase << interior.fileOffset << std::dec << std::endl;
				std::cout << "      Byte size 0x" << std::hex << std::uppercase << interior.fileSize << std::dec << std::endl;
				std::cout << "      Vertex Count " << std::to_string(interior.point.size()) << std::endl;
				std::cout << "      Plane Count " << std::to_string(interior.plane.size()) << std::endl;
				std::cout << "      Surface Count " << std::to_string(interior.surface.size()) << std::endl;
			}

			std::cout << "   Pathed Interior Count: " << std::to_string(dif.subObject.size()) << std::endl;

			for (DIF::U32 i = 0; i < dif.subObject.size(); i ++) {
				const DIF::Interior &interior = dif.subObject[i];
				std::cout << "      Interior " << std::to_string(i) << std::endl;
				std::cout << "      At offset 0x" << std::hex << std::uppercase << interior.fileOffset << std::dec << std::endl;
				std::cout << "      Byte size 0x" << std::hex << std::uppercase << interior.fileSize << std::dec << std::endl;
				std::cout << "      Vertex Count " << std::to_string(interior.point.size()) << std::endl;
				std::cout << "      Plane Count " << std::to_string(interior.plane.size()) << std::endl;
				std::cout << "      Surface Count " << std::to_string(interior.surface.size()) << std::endl;
			}

			std::cout << "   Entity Count: " << std::to_string(dif.gameEntity.size()) << std::endl;

			for (DIF::U32 i = 0; i < dif.gameEntity.size(); i ++) {
				const DIF::GameEntity &entity = dif.gameEntity[i];
				std::cout << "      Entity " << std::to_string(i) << std::endl;
				std::cout << "      At offset 0x" << std::hex << std::uppercase << entity.fileOffset << std::dec << std::endl;
				std::cout << "      Byte size 0x" << std::hex << std::uppercase << entity.fileSize << std::dec << std::endl;
				std::cout << "      Datablock " << entity.datablock << std::endl;
				std::cout << "      Class " << entity.gameClass << std::endl;
				std::cout << "      Position " << entity.position.x << " " << entity.position.y << " " << entity.position.z << std::endl;

				std::cout << "      Property Count: " << std::to_string(entity.properties.size()) << std::endl;
				for (DIF::U32 j = 0; j < entity.properties.size(); j ++) {
					std::cout << "         " << entity.properties[j].first << ": " << entity.properties[j].second << std::endl;
				}
			}

			std::cout << "   Trigger Count: " << std::to_string(dif.trigger.size()) << std::endl;

			for (DIF::U32 i = 0; i < dif.trigger.size(); i ++) {
				const DIF::Trigger &trigger = dif.trigger[i];
				std::cout << "      Trigger " << std::to_string(i) << " / " << trigger.name << std::endl;
				std::cout << "      At offset 0x" << std::hex << std::uppercase << trigger.fileOffset << std::dec << std::endl;
				std::cout << "      Byte size 0x" << std::hex << std::uppercase << trigger.fileSize << std::dec << std::endl;
				std::cout << "      Position " << trigger.offset.x << " " << trigger.offset.y << " " << trigger.offset.z << std::endl;
				std::cout << "      Datablock " << trigger.datablock << std::endl;
				std::cout << "      Polyhedron " << getPolyhedron(trigger) << std::endl;

				std::cout << "      Property Count: " << std::to_string(trigger.properties.size()) << std::endl;
				for (DIF::U32 j = 0; j < trigger.properties.size(); j ++) {
					std::cout << "         " << trigger.properties[j].first << ": " << trigger.properties[j].second << std::endl;
				}
			}
		} else {
			return EXIT_FAILURE;
		}
	}
	return EXIT_SUCCESS;
}
