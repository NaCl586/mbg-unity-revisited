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

#ifndef dif_interior_h
#define dif_interior_h

#include <dif/base/types.h>
#include <dif/objects/staticMesh.h>

DIF_NAMESPACE

static U32 gNumCoordBins = 16;

class Interior : public Readable, public Writable {
public:

	struct Plane : public Readable, public Writable {
		U16 normalIndex;
		F32 planeDistance;

		Plane() : normalIndex(0), planeDistance(0.0f) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct TexGenEq : public Readable, public Writable {
		PlaneF planeX;
		PlaneF planeY;

		TexGenEq() {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct BSPNode : public Readable, public Writable {
		U16 planeIndex;
		U16 frontIndex;
		U16 backIndex;

		BSPNode() : planeIndex(0), frontIndex(0), backIndex(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct BSPSolidLeaf : public Readable, public Writable {
		U32 surfaceIndex;
		U16 surfaceCount;

		BSPSolidLeaf() : surfaceIndex(0), surfaceCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct WindingIndex : public Readable, public Writable {
		U32 windingStart;
		U32 windingCount;

		WindingIndex() : windingStart(0), windingCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Edge : public Readable, public Writable {
		S32 pointIndex0;
		S32 pointIndex1;
		S32 surfaceIndex0;
		S32 surfaceIndex1;

		Edge() : pointIndex0(0), pointIndex1(0), surfaceIndex0(0), surfaceIndex1(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Zone : public Readable, public Writable {
		U16 portalStart;
		U16 portalCount;
		U32 surfaceStart;
		U32 surfaceCount;
		U32 staticMeshStart;
		U32 staticMeshCount;
		U16 flags;

		Zone() : portalStart(0), portalCount(0), surfaceStart(0), surfaceCount(0), staticMeshStart(0), staticMeshCount(0), flags(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Portal : public Readable, public Writable {
		U16 planeIndex;
		U16 triFanCount;
		U32 triFanStart;
		U16 zoneFront;
		U16 zoneBack;

		Portal() : planeIndex(0), triFanCount(0), triFanStart(0), zoneFront(0), zoneBack(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct LightMap : public Readable, public Writable {
		PNG lightMap;
		PNG lightDirMap;
		U8 keepLightMap;

		LightMap() : keepLightMap(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Surface : public Writable {
		
		struct LightMap : public Readable, public Writable {
			U16 finalWord;
			F32 texGenXDistance;
			F32 texGenYDistance;

			LightMap() : finalWord(0), texGenXDistance(0.0f), texGenYDistance(0.0f) {}

			virtual bool read(std::istream &stream, Version &version);
			virtual bool write(std::ostream &stream, Version version) const;
		};

		Surface() : windingStart(0), windingCount(0),
						planeIndex(0), planeFlipped(0),
						textureIndex(0), texGenIndex(0),
						surfaceFlags(0), fanMask(0),
						lightCount(0), lightStateInfoStart(0), 
						mapOffsetX(0), mapOffsetY(0),
						mapSizeX(0), mapSizeY(0),
		                brushId(0) {

		}

		U32 windingStart;
		U32 windingCount;
		U16 planeIndex;
		U8 planeFlipped;
		U16 textureIndex;
		U32 texGenIndex;
		U8 surfaceFlags;
		U32 fanMask;
		LightMap lightMap;
		U16 lightCount;
		U32 lightStateInfoStart;
		U32 mapOffsetX;
		U32 mapOffsetY;
		U32 mapSizeX;
		U32 mapSizeY;
		U32 brushId;

		bool read(std::istream &stream, Version &version, U32 indexSize, U32 planeSize, U32 materialSize, U32 texGenEqSize);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct Edge2 : public Readable, public Writable {
		U32 vertex0;
		U32 vertex1;
		U32 normal0;
		U32 normal1;
		U32 face0;
		U32 face1;

		Edge2() : vertex0(0), vertex1(0), normal0(0), normal1(0), face0(0), face1(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct NullSurface : public Readable, public Writable {
		U32 windingStart;
		U16 planeIndex;
		U8 surfaceFlags;
		U8 windingCount;

		NullSurface() : windingStart(0), planeIndex(0), surfaceFlags(0), windingCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct AnimatedLight : public Readable, public Writable {
		U32 nameIndex;
		U32 stateIndex;
		U16 stateCount;
		U16 flags;
		U32 duration;

		AnimatedLight() : nameIndex(0), stateIndex(0), stateCount(0), flags(0), duration(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct LightState : public Readable, public Writable {
		U8 red;
		U8 green;
		U8 blue;
		U32 activeTime;
		U32 dataIndex;
		U16 dataCount;

		LightState() : red(0), green(0), blue(0), activeTime(0), dataIndex(0), dataCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct StateData : public Readable, public Writable {
		U32 surfaceIndex;
		U32 mapIndex;
		U16 lightStateIndex;

		StateData() : surfaceIndex(0), mapIndex(0), lightStateIndex(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct ConvexHull : public Readable, public Writable {
		U32 hullStart;
		U16 hullCount;
		F32 minX;
		F32 maxX;
		F32 minY;
		F32 maxY;
		F32 minZ;
		F32 maxZ;
		U32 surfaceStart;
		U16 surfaceCount;
		U32 planeStart;
		U32 polyListPlaneStart;
		U32 polyListPointStart;
		U32 polyListStringStart;
		U8 staticMesh;

		ConvexHull() {
			hullStart = 0;
			hullCount = 0;
			minX = 0.0f;
			maxX = 0.0f;
			minY = 0.0f;
			maxY = 0.0f;
			minZ = 0.0f;
			maxZ = 0.0f;
			surfaceStart = 0;
			surfaceCount = 0;
			planeStart = 0;
			polyListPlaneStart = 0;
			polyListPointStart = 0;
			polyListStringStart = 0;
			staticMesh = 0;
		}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	struct CoordBin : public Readable, public Writable {
		U32 binStart;
		U32 binCount;

		CoordBin() : binStart(0), binCount(0) {}

		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};
	
	struct TexMatrix : public Readable, public Writable {
		S32 T;
		S32 N;
		S32 B;

		TexMatrix() : T(0), N(0), B(0) {}
		
		virtual bool read(std::istream &stream, Version &version);
		virtual bool write(std::ostream &stream, Version version) const;
	};

	U32 detailLevel;
	U32 minPixels;
	BoxF boundingBox;
	SphereF boundingSphere;
	U8 hasAlarmState;
	U32 numLightStateEntries;

	std::vector<glm::vec3> normal;
	std::vector<Plane> plane;
	std::vector<glm::vec3> point;
	std::vector<U8> pointVisibility;
	std::vector<TexGenEq> texGenEq;
	std::vector<BSPNode> bspNode;
	std::vector<BSPSolidLeaf> bspSolidLeaf;

	std::vector<std::string> materialName;
	std::vector<U32> index;
	std::vector<WindingIndex> windingIndex;
	std::vector<Edge> edge;
	std::vector<Zone> zone;
	std::vector<U16> zoneSurface;
	std::vector<U32> zoneStaticMesh;
	std::vector<U16> zonePortalList;
	std::vector<Portal> portal;
	std::vector<Surface> surface;
	std::vector<Edge2> edge2;
	std::vector<glm::vec3> normal2;
	std::vector<U16> normalIndex;
	std::vector<U8> normalLMapIndex;
	std::vector<U8> alarmLMapIndex;
	std::vector<NullSurface> nullSurface;
	std::vector<LightMap> lightMap;
	std::vector<U32> solidLeafSurface;
	std::vector<AnimatedLight> animatedLight;
	std::vector<LightState> lightState;
	std::vector<StateData> stateData;
	std::vector<U8> stateDataBuffer;

	U32 flags;

	std::vector<U8> nameBufferCharacter;

	U32 numSubObjects;
	//SubObject *subObject;

	std::vector<ConvexHull> convexHull;
	std::vector<U8> convexHullEmitStringCharacter;
	std::vector<U32> hullIndex;
	std::vector<U16> hullPlaneIndex;
	std::vector<U32> hullEmitStringIndex;
	std::vector<U32> hullSurfaceIndex;
	std::vector<U16> polyListPlaneIndex;
	std::vector<U32> polyListPointIndex;
	std::vector<U8> polyListStringCharacter;
	std::vector<CoordBin> coordBin;
	std::vector<U16> coordBinIndex;

	U32 coordBinMode;
	glm::cvec4 baseAmbientColor;
	glm::cvec4 alarmAmbientColor;

	std::vector<StaticMesh> staticMesh;
	std::vector<glm::vec3> texNormal;
	std::vector<TexMatrix> texMatrix;
	std::vector<U32> texMatIndex;

	U32 extendedLightMapData;
	U32 lightMapBorderSize;

	Interior() {
		detailLevel = 0;
		minPixels = 0;
		hasAlarmState = 0;
		numLightStateEntries = 0;
		flags = 0;
		numSubObjects = 0;
		coordBinMode = 0;
		extendedLightMapData = 0;
		lightMapBorderSize = 0;
	}

	/**
	 * Reads an Interior from a stream
	 * @param stream The stream to read from
	 * @return If the operation was successful
	 */
	bool read(std::istream &stream, Version &version);
	/**
	 * Writes a Interior to a stream
	 * @param stream The stream to write to
	 * @return If the operation was successful
	 */
	bool write(std::ostream &stream, Version version) const;
};

DIF_NAMESPACE_END

#endif
