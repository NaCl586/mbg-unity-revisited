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

#ifndef dif_types_h
#define dif_types_h

#include <fstream>
#include <iostream>
#include <vector>
#include <map>
#include <string>

#include <glm/glm.hpp>
#include <glm/gtc/quaternion.hpp>

namespace glm {
	typedef tvec4<unsigned char, lowp> cvec4;
};

#define DIF_NAMESPACE namespace DIF {
#define DIF_NAMESPACE_END }

DIF_NAMESPACE

//Base types (names stolen from TGE because lazy)
typedef unsigned char      U8;
typedef unsigned short     U16;
typedef unsigned int       U32;
typedef unsigned long long U64;

typedef signed char      S8;
typedef signed short     S16;
typedef signed int       S32;
typedef signed long long S64;

typedef float F32;
typedef double F64;

struct Version {
	struct DIFVersion {
		enum Type {
			Unknown,
			MBG,
			TGE
		} type;
		U32 version;

		DIFVersion(U32 version = 44, Type type = Unknown) : type(type), version(version) {

		}
		std::string to_string() {
			switch (type) {
				case Unknown: return std::to_string(version) + " (Unknown)";
				case MBG:     return std::to_string(version) + " (MBG)";
				case TGE:     return std::to_string(version) + " (TGE)";
			}
		}
	} dif;
	struct InteriorVersion {
		enum Type {
			Unknown,
			MBG,
			TGE,
			TGEA
		} type;
		U32 version;

		InteriorVersion(U32 version = 0, Type type = Unknown) : type(type), version(version) {

		}
		std::string to_string() {
			switch (type) {
				case Unknown: return std::to_string(version) + " (Unknown)";
				case MBG:     return std::to_string(version) + " (MBG)";
				case TGE:     return std::to_string(version) + " (TGE)";
				case TGEA:    return std::to_string(version) + " (TGEA)";
			}
		}
		bool isTGE() {
			return type == TGE || type == MBG;
		}
	} interior;
	struct MaterialListVersion {
		U32 version;
		MaterialListVersion(U32 version = 1) : version(version) {

		}
		std::string to_string() {
			return std::to_string(version);
		}
	} material;
	struct VehicleCollisionFileVersion {
		U32 version;
		VehicleCollisionFileVersion(U32 version = 0) : version(version) {

		}
		std::string to_string() {
			return std::to_string(version);
		}
	} vehicleCollision;

	Version(DIFVersion difVersion = DIFVersion(), InteriorVersion interiorVersion = InteriorVersion(), MaterialListVersion materialVersion = MaterialListVersion(), VehicleCollisionFileVersion vehicleCollisionVersion = VehicleCollisionFileVersion()) :
		dif(difVersion),
		interior(interiorVersion),
		material(materialVersion),
		vehicleCollision(vehicleCollisionVersion) {

	}
};

struct Readable {
public:
	int fileOffset;
	int fileSize;
	virtual bool read(std::istream &stream, Version &version) = 0;
};

struct Writable {
public:
	virtual bool write(std::ostream &stream, Version version) const = 0;
};

typedef std::vector<std::pair<std::string, std::string>> Dictionary;

//More names stolen from TGE

class PlaneF : public Readable, public Writable {
public:
	F32 x;
	F32 y;
	F32 z;
	F32 d;

	PlaneF(F32 x, F32 y, F32 z, F32 d) : x(x), y(y), z(z), d(d) {}
	PlaneF() : x(0.0f), y(0.0f), z(0.0f), d(0.0f) {}

	virtual bool read(std::istream &stream, Version &version);
	virtual bool write(std::ostream &stream, Version version) const;
};

class BoxF : public Readable, public Writable {
public:
	F32 minX;
	F32 minY;
	F32 minZ;
	F32 maxX;
	F32 maxY;
	F32 maxZ;

	BoxF() : minX(0.0f), minY(0.0f), minZ(0.0f), maxX(0.0f), maxY(0.0f), maxZ(0.0f) {}
	BoxF(F32 minX, F32 minY, F32 minZ, F32 maxX, F32 maxY, F32 maxZ) : minX(minX), minY(minY), minZ(minZ), maxX(maxX), maxY(maxY), maxZ(maxZ) {}

	inline glm::vec3 getMin() const {
		return glm::vec3(minX, minY, minZ);
	}
	inline glm::vec3 getMax() const {
		return glm::vec3(maxX, maxY, maxZ);
	}
	inline glm::vec3 getCenter() const {
		return (getMax() + getMin()) / 2.0f;
	}
	inline void unionPoint(const glm::vec3 &point) {
		if (point.x < minX) minX = point.x;
		if (point.y < minY) minY = point.y;
		if (point.z < minZ) minZ = point.z;
		if (point.x > maxX) maxX = point.x;
		if (point.y > maxY) maxY = point.y;
		if (point.z > maxZ) maxZ = point.z;
	}
	inline BoxF operator*(const F32 &scale) {
		BoxF newBox;
		glm::vec3 center = getCenter();
		newBox.minX = center.x - ((center.x - minX) / scale);
		newBox.minY = center.y - ((center.y - minY) / scale);
		newBox.minZ = center.z - ((center.z - minZ) / scale);

		newBox.maxX = center.x + ((maxX - center.x) / scale);
		newBox.maxY = center.y + ((maxY - center.y) / scale);
		newBox.maxZ = center.z + ((maxZ - center.z) / scale);

		return newBox;
	}
	inline BoxF operator*=(const F32 &scale) {
		glm::vec3 center = getCenter();
		minX = center.x - ((center.x - minX) / scale);
		minY = center.y - ((center.y - minY) / scale);
		minZ = center.z - ((center.z - minZ) / scale);

		maxX = center.x + ((maxX - center.x) / scale);
		maxY = center.y + ((maxY - center.y) / scale);
		maxZ = center.z + ((maxZ - center.z) / scale);

		return *this;
	}

	virtual bool read(std::istream &stream, Version &version);
	virtual bool write(std::ostream &stream, Version version) const;
};

class SphereF : public Readable, public Writable {
public:
	F32 x;
	F32 y;
	F32 z;
	F32 radius;

	SphereF() : x(0.0f), y(0.0f), z(0.0f), radius(0.0f) {}

	virtual bool read(std::istream &stream, Version &version);
	virtual bool write(std::ostream &stream, Version version) const;
};

class PNG : public Readable, public Writable {
public:
	std::vector<U8> data;

	PNG() {}

	virtual bool read(std::istream &stream, Version &version);
	virtual bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
