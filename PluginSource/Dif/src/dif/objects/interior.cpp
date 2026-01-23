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

//Warning: This is the scary file.

#include <assert.h>
#include <dif/base/io.h>
#include <dif/objects/interior.h>

DIF_NAMESPACE

bool Interior::read(std::istream &stream, Version &version) {
	//Assume TGEA
	if (version.interior.type == Version::InteriorVersion::Type::Unknown) {
		version.interior.type = Version::InteriorVersion::Type::TGEA;
	}

	READCHECK(version.interior.version, U32); //interiorFileVersion
	if (version.interior.version > 14) {
		//Cannot support these
		return false;
	}

	READCHECK(detailLevel, U32); //detailLevel
	READCHECK(minPixels, U32); //minPixels
	READCHECK(boundingBox, BoxF); //boundingBox
	READCHECK(boundingSphere, SphereF); //boundingSphere
	READCHECK(hasAlarmState, U8); //hasAlarmState
	READCHECK(numLightStateEntries, U32); //numLightStateEntries
	READCHECK(normal, std::vector<glm::vec3>); //normal
	READCHECK(plane, std::vector<Plane>); //plane
	READCHECK(point, std::vector<glm::vec3>); //point
	if (version.interior.version == 4) { //They exist in 0, 2, 3 but not 4
		//Probably defaulted to FF but uncertain
	} else {
		READCHECK(pointVisibility, std::vector<U8>); //pointVisibility
	}
	READCHECK(texGenEq, std::vector<TexGenEq>); //texGenEq
	READCHECK(bspNode, std::vector<BSPNode>); //BSPNode
	READCHECK(bspSolidLeaf, std::vector<BSPSolidLeaf>); //BSPSolidLeaf
	//MaterialList
	READCHECK(version.material.version, U8); //version
	READCHECK(materialName, std::vector<std::string>); //materialName
	if (!IO::read_as<U32, U16>(stream, version, index, [](bool, U32 param)->bool{return param ? true : false;}, "index")) return false; //index
	READCHECK(windingIndex, std::vector<WindingIndex>); //windingIndex
	if (version.interior.version >= 12) {
		READCHECK(edge, std::vector<Edge>); //edge
	}
	READCHECK(zone, std::vector<Zone>);//zone
	if (!IO::read_as<U16, U16>(stream, version, zoneSurface, [](bool, U32)->bool{return false;}, "zoneSurface")) return false; //zoneSurface
	if (version.interior.version >= 12) {
		READCHECK(zoneStaticMesh, std::vector<U32>); //zoneStaticMesh
	}
	if (!IO::read_as<U16, U16>(stream, version, zonePortalList, [](bool, U32)->bool{return false;}, "zonePortalList")) return false; //zonePortalList
	READCHECK(portal, std::vector<Portal>); //portal

	//Ok so Torque needs to fuck themselves in the ass, multiple times.
	// They have two "version 0"s, one for TGE and one for TGEA. So, you
	// might ask, how do they tell which is which? I'll tell you: They
	// read the surfaces, and if anything is wrong, they fall back on the
	// old format. So guess what I get to do? Yep! That!

	//Save the file position as we'll need to rewind if any reads fail
	std::istream::pos_type pos = stream.tellg();

	if (IO::read_with<Surface>(stream, version, surface, [&](Surface &surface, std::istream &stream, Version &version)->bool{
		return surface.read(stream, version, static_cast<U32>(index.size()), static_cast<U32>(plane.size()), static_cast<U32>(materialName.size()), static_cast<U32>(texGenEq.size()));
	}, "surface")) { //surface
		//Read sucessfully as a T3D/TGEA dif
		if (version.dif.type == Version::DIFVersion::Type::Unknown) {
			version.dif.type = Version::DIFVersion::Type::TGE;
		}
	} else {
		//Not a TGE/TGEA dif, probably MBG. Not really sure if we can detect the
		// difference here, needs more error checking.
		if (version.dif.type == Version::DIFVersion::Type::Unknown) {
			//TODO: Assume TGE until we can determine that it must be MBG
			version.dif.type = Version::DIFVersion::Type::MBG;
			version.interior.type = Version::InteriorVersion::Type::MBG;
		}

		if (version.interior.version != 0) {
			//Oh fuck oh fuck, TGE interiors only have version 0
			//This means that readSurface failed on a TGEA interior

			//Bail
			return false;
		}
		//Ok so we failed reading, it's *probably* a TGE interior. Let's try
		// to read it as a TGE interior.

		//First, rewind
		stream.seekg(pos);

		//Second clean up
		surface.clear();

		//Third, re-read
		if (!IO::read_with<Surface>(stream, version, surface, [&](Surface &surface, std::istream &stream, Version &version)->bool{
			return surface.read(stream, version, static_cast<U32>(index.size()), static_cast<U32>(plane.size()), static_cast<U32>(materialName.size()), static_cast<U32>(texGenEq.size()));
		}, "surface")) { //surface
			//Ok this surface failed too. Bail.
			return false;
		}
	}

	if (version.interior.version >= 2 && version.interior.version <= 5) {
		//Edge data from MBU levels and beyond in some cases
		READCHECK(edge2, std::vector<Edge2>); //edge2

		//v4 has some extra points and indices, they're probably used with the edges
		// but I have no idea
		if (version.interior.version >= 4 && version.interior.version <= 5) {
			//Extra normals used in reading the edges?
			READCHECK(normal2, std::vector<glm::vec3>); //normal2

			//Looks like indcies of some sort, can't seem to make them out though
			
			//Unlike anywhere else, these actually take the param into account.
			// If it's read2 and param == 0, then they use U8s, if param == 1, they use U16s
			// Not really sure why, haven't seen this anywhere else.

			if (!IO::read_as<U16, U8>(stream, version, normalIndex, [](bool useAlternate, U32 param)->bool{return (useAlternate && param == 0);}, "normalIndex")) return false; //normalIndex
		}
	}
	if (version.interior.version == 4) { //Found in 0, 2, 3, and TGE (14)
		READCHECK(normalLMapIndex, std::vector<U8>); //normalLMapIndex
	} else if (version.interior.version >= 13) {
		//These are 32-bit values in v13 and up
		if (!IO::read_as<U8, U32>(stream, version, normalLMapIndex, [](bool, U32)->bool{return true;}, "normalLMapIndex")) return false; //normalLMapIndex
		if (!IO::read_as<U8, U32>(stream, version, alarmLMapIndex, [](bool, U32)->bool{return true;}, "alarmLMapIndex")) return false; //alarmLMapIndex
	} else {
		//Normally they're just 8
		READCHECK(normalLMapIndex, std::vector<U8>); //normalLMapIndex
		READCHECK(alarmLMapIndex, std::vector<U8>); //alarmLMapIndex
	}
	READCHECK(nullSurface, std::vector<NullSurface>); //nullSurface
	if (version.interior.version != 4) { //Also found in 0, 2, 3, 14
		READCHECK(lightMap, std::vector<LightMap>); //lightMap
		if (lightMap.size() > 0 && version.interior.type == Version::InteriorVersion::Type::MBG)
			version.interior.type = Version::InteriorVersion::Type::TGE;
	}
	if (!IO::read_as<U32, U16>(stream, version, solidLeafSurface, [](bool useAlternate, U32)->bool{return useAlternate;}, "solidLeafSurface")) return false; //solidLeafSurface
	READCHECK(animatedLight, std::vector<AnimatedLight>); //animatedLight
	READCHECK(lightState, std::vector<LightState>); //lightState
	if (version.interior.version == 4) { //Yet more things found in 0, 2, 3, 14
		flags = 0;
		numSubObjects = 0;
	} else {
		READCHECK(stateData, std::vector<StateData>); //stateData

		//State datas have the flags field written right after the vector size,
		// and THEN the data, just to make things confusing. So we need yet another
		// read method for this.
		if (!IO::read_extra(stream, version, stateDataBuffer, [&](std::istream &stream, Version &version)->bool{
			return IO::read(stream, version, flags, "flags"); //flags
		}, "stateDataBuffer")) return false; //stateDataBuffer
		READCHECK(nameBufferCharacter, std::vector<U8>); //nameBufferCharacter

		READCHECK(numSubObjects, U32); //numSubObjects
		if (numSubObjects != 0) { //Can't support these currently
			return false;
		}
//		READLOOP(numSubObjects) {
//			//NFC
//		}
	}
	READCHECK(convexHull, std::vector<ConvexHull>); //convexHull
	READCHECK(convexHullEmitStringCharacter, std::vector<U8>); //convexHullEmitStringCharacter

	//-------------------------------------------------------------------------
	// Lots of index lists here that have U16 or U32 versions based on loop2.
	// The actual bytes of the interior have 0x80s at the ends (negative bit)
	// which seems to specify that these take a smaller type. They managed to
	// save ~50KB/interior, but was it worth the pain?
	//
	// Also fun fact: the U16 lists have literally no reason for the 0x80, as
	// they're already using U16s. However, GG still puts them in. What the
	// fuck, GarageGames?
	//-------------------------------------------------------------------------

	if (!IO::read_as<U32, U16>(stream, version, hullIndex, [](bool useAlternate, U32)->bool{return useAlternate;}, "hullIndex")) return false; //hullIndex
	if (!IO::read_as<U16, U16>(stream, version, hullPlaneIndex, [](bool, U32)->bool{return true;}, "hullPlaneIndex")) return false; //hullPlaneIndex
	if (!IO::read_as<U32, U16>(stream, version, hullEmitStringIndex, [](bool useAlternate, U32)->bool{return useAlternate;}, "hullEmitStringIndex")) return false; //hullEmitStringIndex
	if (!IO::read_as<U32, U16>(stream, version, hullSurfaceIndex, [](bool useAlternate, U32)->bool{return useAlternate;}, "hullSurfaceIndex")) return false; //hullSurfaceIndex
	if (!IO::read_as<U16, U16>(stream, version, polyListPlaneIndex, [](bool, U32)->bool{return true;}, "polyListPlaneIndex")) return false; //polyListPlaneIndex
	if (!IO::read_as<U32, U16>(stream, version, polyListPointIndex, [](bool useAlternate, U32)->bool{return useAlternate;}, "polyListPointIndex")) return false; //polyListPointIndex
	//Not sure if this should be a read_as, but I haven't seen any evidence
	// of needing that for U8 lists.
	READCHECK(polyListStringCharacter, std::vector<U8>); //polyListStringCharacter

	coordBin.reserve(gNumCoordBins * gNumCoordBins);
	for (U32 i = 0; i < gNumCoordBins * gNumCoordBins; i ++) {
		CoordBin bin;
		if (IO::read(stream, version, bin, "coordBin")) //coordBin
			coordBin.push_back(bin);
		else
			return false;
	}

	if (!IO::read_as<U16, U16>(stream, version, coordBinIndex, [](bool, U32)->bool{return true;}, "coordBinIndex")) return false; //coordBinIndex
	READCHECK(coordBinMode, U32); //coordBinMode
	if (version.interior.version == 4) { //All of this is missing in v4 as well. Saves no space.
		baseAmbientColor = glm::cvec4(0, 0, 0, 255);
		alarmAmbientColor = glm::cvec4(0, 0, 0, 255);
		extendedLightMapData = 0;
		lightMapBorderSize = 0;
	} else {
		READCHECK(baseAmbientColor, glm::cvec4); //baseAmbientColor
		READCHECK(alarmAmbientColor, glm::cvec4); //alarmAmbientColor

		if (version.interior.version >= 10) {
			READCHECK(staticMesh, std::vector<StaticMesh>); //staticMesh
		}
		if (version.interior.version >= 11) {
			READCHECK(texNormal, std::vector<glm::vec3>); //texNormal
			READCHECK(texMatrix, std::vector<TexMatrix>); //texMatrix
			READCHECK(texMatIndex, std::vector<U32>); //texMatIndex
		} else {
			U32 numTexNormals;
			U32 numTexMatrices;
			U32 numTexMatIndices;

			READCHECK(numTexNormals, U32); //numTexNormals
			READCHECK(numTexMatrices, U32); //numTexMatrices
			READCHECK(numTexMatIndices, U32); //numTexMatIndices
		}
		READCHECK(extendedLightMapData, U32); //extendedLightMapData
		if (extendedLightMapData) {
			READCHECK(lightMapBorderSize, U32); //lightMapBorderSize
			U32 dummy;
			READCHECK(dummy, U32); //dummy
		} else {
			lightMapBorderSize = 0;
		}
	}

	return true;
}

bool Interior::write(std::ostream &stream, Version version) const {
	//We can only write version 0 maps at the moment.
	WRITECHECK(version.interior.version, U32); //interiorFileVersion
	WRITECHECK(detailLevel, U32); //detailLevel
	WRITECHECK(minPixels, U32); //minPixels
	WRITECHECK(boundingBox, BoxF); //boundingBox
	WRITECHECK(boundingSphere, SphereF); //boundingSphere
	WRITECHECK(hasAlarmState, U8); //hasAlarmState
	WRITECHECK(numLightStateEntries, U32); //numLightStateEntries
	WRITECHECK(normal, std::vector<glm::vec3>); //normal
	WRITECHECK(plane, std::vector<Plane>); //numPlanes
	WRITECHECK(point, std::vector<glm::vec3>); //point
	WRITECHECK(pointVisibility, std::vector<U8>); //pointVisibility
	WRITECHECK(texGenEq, std::vector<TexGenEq>); //texGenEq
	WRITECHECK(bspNode, std::vector<BSPNode>); //BSPNode
	WRITECHECK(bspSolidLeaf, std::vector<BSPSolidLeaf>); //BSPSolidLeaf
	WRITECHECK(version.material.version, U8); //materialListVersion
	WRITECHECK(materialName, std::vector<std::string>); //material
	WRITECHECK(index, std::vector<U32>); //index
	WRITECHECK(windingIndex, std::vector<WindingIndex>); //windingIndex
	WRITECHECK(zone, std::vector<Zone>); //zone
	WRITECHECK(zoneSurface, std::vector<U16>); //zoneSurface
	WRITECHECK(zonePortalList, std::vector<U16>); //zonePortalList
	WRITECHECK(portal, std::vector<Portal>); //portal
	WRITECHECK(surface, std::vector<Surface>); //surface
	WRITECHECK(normalLMapIndex, std::vector<U8>); //normalLMapIndex
	WRITECHECK(alarmLMapIndex, std::vector<U8>); //alarmLMapIndex
	WRITECHECK(nullSurface, std::vector<NullSurface>); //nullSurface
	if (version.interior.type == Version::InteriorVersion::Type::MBG) {
		WRITECHECK(0, U32); //lightMap
	} else {
		WRITECHECK(lightMap, std::vector<LightMap>); //lightMap
	}
	WRITECHECK(solidLeafSurface, std::vector<U32>); //solidLeafSurface
	WRITECHECK(animatedLight, std::vector<AnimatedLight>); //animatedLight
	WRITECHECK(lightState, std::vector<LightState>); //lightState
	WRITECHECK(stateData, std::vector<StateData>); //stateData
	IO::write_extra(stream, version, stateDataBuffer, [&](std::ostream &stream, Version version)->bool {
		return IO::write(stream, version, flags, "flags"); //flags
	}, "stateDataBuffer"); //stateDataBuffer
	WRITECHECK(nameBufferCharacter, std::vector<U8>); //nameBufferCharacter
	WRITECHECK(numSubObjects, U32); //numSubObjects
//	WRITELOOP(numSubObjects) {} //subObject
	WRITECHECK(convexHull, std::vector<ConvexHull>); //convexHull
	WRITECHECK(convexHullEmitStringCharacter, std::vector<U8>); //convexHullEmitStringCharacter
	WRITECHECK(hullIndex, std::vector<U32>); //hullIndex
	WRITECHECK(hullPlaneIndex, std::vector<U16>); //hullPlaneIndex
	WRITECHECK(hullEmitStringIndex, std::vector<U32>); //hullEmitStringIndex
	WRITECHECK(hullSurfaceIndex, std::vector<U32>); //hullSurfaceIndex
	WRITECHECK(polyListPlaneIndex, std::vector<U16>); //polyListPlaneIndex
	WRITECHECK(polyListPointIndex, std::vector<U32>); //polyListPointIndex
	WRITECHECK(polyListStringCharacter, std::vector<U8>); //polyListStringCharacter
	for (U32 i = 0; i < gNumCoordBins * gNumCoordBins; i ++) {
		WRITECHECK(coordBin[i], CoordBin); //coordBin
	}
	WRITECHECK(coordBinIndex, std::vector<U16>); //coordBinIndex
	WRITECHECK(coordBinMode, U32); //coordBinMode
	WRITECHECK(baseAmbientColor, glm::cvec4); //baseAmbientColor
	WRITECHECK(alarmAmbientColor, glm::cvec4); //alarmAmbientColor
	/*
	 Static meshes (not included)
	 */
	if (version.interior.type == Version::InteriorVersion::Type::MBG) {
		WRITECHECK(0, U32); //texNormal
		WRITECHECK(0, U32); //texMatrix
		WRITECHECK(0, U32); //texMatIndex
	} else {
		WRITECHECK(texNormal, std::vector<glm::vec3>); //texNormal
		WRITECHECK(texMatrix, std::vector<TexMatrix>); //texMatrix
		WRITECHECK(texMatIndex, std::vector<U32>); //texMatIndex
	}
	WRITECHECK(0, U32); //extendedLightMapData
//	WRITECHECK(extendedLightMapData, U32); //extendedLightMapData

	return true;
}

//----------------------------------------------------------------------------

bool Interior::Plane::read(std::istream &stream, Version &version) {
	READCHECK(normalIndex, U16); //normalIndex
	READCHECK(planeDistance, F32); //planeDistance
	return true;
}

bool Interior::Plane::write(std::ostream &stream, Version version) const {
	WRITECHECK(normalIndex, U16); //normalIndex
	WRITECHECK(planeDistance, F32); //planeDistance
	return true;
}

bool Interior::TexGenEq::read(std::istream &stream, Version &version) {
	READCHECK(planeX, PlaneF); //planeX
	READCHECK(planeY, PlaneF); //planeY
	return true;
}

bool Interior::TexGenEq::write(std::ostream &stream, Version version) const {
	WRITECHECK(planeX, PlaneF); //planeX
	WRITECHECK(planeY, PlaneF); //planeY
	return true;
}

bool Interior::BSPNode::read(std::istream &stream, Version &version) {
	READCHECK(planeIndex, U16); //planeIndex
	if (version.interior.version >= 14) {
		U32 tmpFront, tmpBack;
		READCHECK(tmpFront, U32); //frontIndex
		READCHECK(tmpBack, U32); //backIndex

		//Fuckers
		if ((tmpFront & 0x80000) != 0) {
			tmpFront = (tmpFront & ~0x80000) | 0x8000;
		}
		if ((tmpFront & 0x40000) != 0) {
			tmpFront = (tmpFront & ~0x40000) | 0x4000;
		}
		if ((tmpBack & 0x80000) != 0) {
			tmpBack = (tmpBack & ~0x80000) | 0x8000;
		}
		if ((tmpBack & 0x40000) != 0) {
			tmpBack = (tmpBack & ~0x40000) | 0x4000;
		}

		frontIndex = tmpFront;
		backIndex = tmpBack;
	} else {
		READCHECK(frontIndex, U16); //frontIndex
		READCHECK(backIndex, U16); //backIndex
	}
	return true;
}

bool Interior::BSPNode::write(std::ostream &stream, Version version) const {
	WRITECHECK(planeIndex, U16); //planeIndex
	WRITECHECK(frontIndex, U16); //frontIndex
	WRITECHECK(backIndex, U16); //backIndex
	return true;
}

bool Interior::BSPSolidLeaf::read(std::istream &stream, Version &version) {
	READCHECK(surfaceIndex, U32); //surfaceIndex
	READCHECK(surfaceCount, U16); //surfaceCount
	return true;
}

bool Interior::BSPSolidLeaf::write(std::ostream &stream, Version version) const {
	WRITECHECK(surfaceIndex, U32); //surfaceIndex
	WRITECHECK(surfaceCount, U16); //surfaceCount
	return true;
}

bool Interior::WindingIndex::read(std::istream &stream, Version &version) {
	READCHECK(windingStart, U32); //windingStart
	READCHECK(windingCount, U32); //windingCount
	return true;
}

bool Interior::WindingIndex::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(windingCount, U32); //windingCount
	return true;
}

bool Interior::Zone::read(std::istream &stream, Version &version) {
	READCHECK(portalStart, U16); //portalStart
	READCHECK(portalCount, U16); //portalCount
	READCHECK(surfaceStart, U32); //surfaceStart
	READCHECK(surfaceCount, U32); //surfaceCount
	if (version.interior.version >= 12) {
		READCHECK(staticMeshStart, U32); //staticMeshStart
		READCHECK(staticMeshCount, U32); //staticMeshCount
//		READCHECK(flags, U16); //flags
	} else {
		staticMeshStart = 0;
		staticMeshCount = 0;
		flags = 0;
	}
	return true;
}

bool Interior::Zone::write(std::ostream &stream, Version version) const {
	WRITECHECK(portalStart, U16); //portalStart
	WRITECHECK(portalCount, U16); //portalCount
	WRITECHECK(surfaceStart, U32); //surfaceStart
	WRITECHECK(surfaceCount, U32); //surfaceCount
	return true;
}

bool Interior::Edge::read(std::istream &stream, Version &version) {
	READCHECK(pointIndex0, S32); //pointIndex0
	READCHECK(pointIndex1, S32); //pointIndex1
	READCHECK(surfaceIndex0, S32); //surfaceIndex0
	READCHECK(surfaceIndex1, S32); //surfaceIndex1
	return true;
}

bool Interior::Edge::write(std::ostream &stream, Version version) const {
	WRITECHECK(pointIndex0, S32); //pointIndex0
	WRITECHECK(pointIndex1, S32); //pointIndex1
	WRITECHECK(surfaceIndex0, S32); //surfaceIndex0
	WRITECHECK(surfaceIndex1, S32); //surfaceIndex1
	return true;
}

bool Interior::Portal::read(std::istream &stream, Version &version) {
	READCHECK(planeIndex, U16); //planeIndex
	READCHECK(triFanCount, U16); //triFanCount
	READCHECK(triFanStart, U32); //triFanStart
	READCHECK(zoneFront, U16); //zoneFront
	READCHECK(zoneBack, U16); //zoneBack
	return true;
}

bool Interior::Portal::write(std::ostream &stream, Version version) const {
	WRITECHECK(planeIndex, U16); //planeIndex
	WRITECHECK(triFanCount, U16); //triFanCount
	WRITECHECK(triFanStart, U32); //triFanStart
	WRITECHECK(zoneFront, U16); //zoneFront
	WRITECHECK(zoneBack, U16); //zoneBack
	return true;
}

bool Interior::Surface::LightMap::read(std::istream &stream, Version &version) {
	READCHECK(finalWord, U16); //finalWord
	READCHECK(texGenXDistance, F32); //texGenXDistance
	READCHECK(texGenYDistance, F32); //texGenYDistance
	return true;
}

bool Interior::Surface::LightMap::write(std::ostream &stream, Version version) const {
	WRITECHECK(finalWord, U16); //finalWord
	WRITECHECK(texGenXDistance, F32); //texGenXDistance
	WRITECHECK(texGenYDistance, F32); //texGenYDistance
	return true;
}

bool Interior::Surface::read(std::istream &stream, Version &version, U32 indexSize, U32 planeSize, U32 materialSize, U32 texGenEqSize) {
	READCHECK(windingStart, U32); //windingStart
	if (version.interior.version >= 13) {
		READCHECK(windingCount, U32); //windingCount
	} else {
		READCHECK(windingCount, U8); //windingCount
	}
	if (windingStart + windingCount > indexSize)
		return false;

	//Fucking GarageGames. Sometimes the plane is | 0x8000 because WHY NOT
	S16 plane;
	READCHECK(plane, S16); //planeIndex
	//Ugly hack
	planeFlipped = (plane >> 15 != 0);
	plane &= ~0x8000;
	planeIndex = plane;
	if (planeIndex > planeSize)
		return false;

	READCHECK(textureIndex, U16); //textureIndex
	if (textureIndex > materialSize)
		return false;

	READCHECK(texGenIndex, U32); //texGenIndex
	if (texGenIndex > texGenEqSize)
		return false;

	READCHECK(surfaceFlags, U8); //surfaceFlags
	READCHECK(fanMask, U32); //fanMask
	READCHECK(lightMap, LightMap); //lightMap
	READCHECK(lightCount, U16); //lightCount
	READCHECK(lightStateInfoStart, U32); //lightStateInfoStart

	if (version.interior.version >= 13) {
		READCHECK(mapOffsetX, U32); //mapOffsetX
		READCHECK(mapOffsetY, U32); //mapOffsetY
		READCHECK(mapSizeX, U32); //mapSizeX
		READCHECK(mapSizeY, U32); //mapSizeY
	} else {
		READCHECK(mapOffsetX, U8); //mapOffsetX
		READCHECK(mapOffsetY, U8); //mapOffsetY
		READCHECK(mapSizeX, U8); //mapSizeX
		READCHECK(mapSizeY, U8); //mapSizeY
	}

	if (!version.interior.isTGE()) {
		U8 unused;
		READCHECK(unused, U8); //unused
		if (version.interior.version >= 2 && version.interior.version <= 5) {
			READCHECK(brushId, U32); //brushId
		}
	}
	return true;
}

bool Interior::Surface::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(windingCount, U8); //windingCount
	U16 index = planeIndex;
	if (planeFlipped)
		index |= 0x8000;
	WRITECHECK(index, U16); //planeIndex
	WRITECHECK(textureIndex, U16); //textureIndex
	WRITECHECK(texGenIndex, U32); //texGenIndex
	WRITECHECK(surfaceFlags, U8); //surfaceFlags
	WRITECHECK(fanMask, U32); //fanMask
	WRITECHECK(lightMap, LightMap); //lightMap
	WRITECHECK(lightCount, U16); //lightCount
	WRITECHECK(lightStateInfoStart, U32); //lightStateInfoStart
	WRITECHECK(mapOffsetX, U8); //mapOffsetX
	WRITECHECK(mapOffsetY, U8); //mapOffsetY
	WRITECHECK(mapSizeX, U8); //mapSizeX
	WRITECHECK(mapSizeY, U8); //mapSizeY
	return true;
}

bool Interior::Edge2::read(std::istream &stream, Version &version) {
	READCHECK(vertex0, U32);
	READCHECK(vertex1, U32);
	READCHECK(normal0, U32);
	READCHECK(normal1, U32);
	if (version.interior.version >= 3) {
		READCHECK(face0, U32);
		READCHECK(face1, U32);
	}
	return true;
}

bool Interior::Edge2::write(std::ostream &stream, Version version) const {
	WRITECHECK(vertex0, U32);
	WRITECHECK(vertex1, U32);
	WRITECHECK(normal0, U32);
	WRITECHECK(normal1, U32);
	if (version.interior.version >= 3) {
		WRITECHECK(face0, U32);
		WRITECHECK(face1, U32);
	}
	return true;
}

bool Interior::NullSurface::read(std::istream &stream, Version &version) {
	READCHECK(windingStart, U32); //windingStart
	READCHECK(planeIndex, U16); //planeIndex
	READCHECK(surfaceFlags, U8); //surfaceFlags
	if (version.interior.version >= 13) {
		READCHECK(windingCount, U32); //windingCount
	} else {
		READCHECK(windingCount, U8); //windingCount
	}
	return true;
}

bool Interior::NullSurface::write(std::ostream &stream, Version version) const {
	WRITECHECK(windingStart, U32); //windingStart
	WRITECHECK(planeIndex, U16); //planeIndex
	WRITECHECK(surfaceFlags, U8); //surfaceFlags
	WRITECHECK(windingCount, U8); //windingCount
	return true;
}

bool Interior::LightMap::read(std::istream &stream, Version &version) {
	READCHECK(lightMap, PNG); //lightMap
	if (!version.interior.isTGE()) {
		//These aren't even used in the real game!
		READCHECK(lightDirMap, PNG); //lightDirMap
	}
	READCHECK(keepLightMap, U8); //keepLightMap
	return true;
}

bool Interior::LightMap::write(std::ostream &stream, Version version) const {
	WRITECHECK(lightMap, PNG); //lightMap
	WRITECHECK(keepLightMap, U8); //keepLightMap
	return true;
}

bool Interior::AnimatedLight::read(std::istream &stream, Version &version) {
	READCHECK(nameIndex, U32); //nameIndex
	READCHECK(stateIndex, U32); //stateIndex
	READCHECK(stateCount, U16); //stateCount
	READCHECK(flags, U16); //flags
	READCHECK(duration, U32); //duration
	return true;
}

bool Interior::AnimatedLight::write(std::ostream &stream, Version version) const {
	WRITECHECK(nameIndex, U32); //nameIndex
	WRITECHECK(stateIndex, U32); //stateIndex
	WRITECHECK(stateCount, U16); //stateCount
	WRITECHECK(flags, U16); //flags
	WRITECHECK(duration, U32); //duration
	return true;
}

bool Interior::LightState::read(std::istream &stream, Version &version) {
	READCHECK(red, U8); //red
	READCHECK(green, U8); //green
	READCHECK(blue, U8); //blue
	READCHECK(activeTime, U32); //activeTime
	READCHECK(dataIndex, U32); //dataIndex
	READCHECK(dataCount, U16); //dataCount
	return true;
}

bool Interior::LightState::write(std::ostream &stream, Version version) const {
	WRITECHECK(red, U8); //red
	WRITECHECK(green, U8); //green
	WRITECHECK(blue, U8); //blue
	WRITECHECK(activeTime, U32); //activeTime
	WRITECHECK(dataIndex, U32); //dataIndex
	WRITECHECK(dataCount, U16); //dataCount
	return true;
}

bool Interior::StateData::read(std::istream &stream, Version &version) {
	READCHECK(surfaceIndex, U32); //surfaceIndex
	READCHECK(mapIndex, U32); //mapIndex
	READCHECK(lightStateIndex, U16); //lightStateIndex
	return true;
}

bool Interior::StateData::write(std::ostream &stream, Version version) const {
	WRITECHECK(surfaceIndex, U32); //surfaceIndex
	WRITECHECK(mapIndex, U32); //mapIndex
	WRITECHECK(lightStateIndex, U16); //lightStateIndex
	return true;
}

bool Interior::ConvexHull::read(std::istream &stream, Version &version) {
	READCHECK(hullStart, U32); //hullStart
	READCHECK(hullCount, U16); //hullCount
	READCHECK(minX, F32); //minX
	READCHECK(maxX, F32); //maxX
	READCHECK(minY, F32); //minY
	READCHECK(maxY, F32); //maxY
	READCHECK(minZ, F32); //minZ
	READCHECK(maxZ, F32); //maxZ
	READCHECK(surfaceStart, U32); //surfaceStart
	READCHECK(surfaceCount, U16); //surfaceCount
	READCHECK(planeStart, U32); //planeStart
	READCHECK(polyListPlaneStart, U32); //polyListPlaneStart
	READCHECK(polyListPointStart, U32); //polyListPointStart
	READCHECK(polyListStringStart, U32); //polyListStringStart

	if (version.interior.version >= 12) {
		READCHECK(staticMesh, U8); //staticMesh
	} else {
		staticMesh = 0;
	}
	return true;
}

bool Interior::ConvexHull::write(std::ostream &stream, Version version) const {
	WRITECHECK(hullStart, U32); //hullStart
	WRITECHECK(hullCount, U16); //hullCount
	WRITECHECK(minX, F32); //minX
	WRITECHECK(maxX, F32); //maxX
	WRITECHECK(minY, F32); //minY
	WRITECHECK(maxY, F32); //maxY
	WRITECHECK(minZ, F32); //minZ
	WRITECHECK(maxZ, F32); //maxZ
	WRITECHECK(surfaceStart, U32); //surfaceStart
	WRITECHECK(surfaceCount, U16); //surfaceCount
	WRITECHECK(planeStart, U32); //planeStart
	WRITECHECK(polyListPlaneStart, U32); //polyListPlaneStart
	WRITECHECK(polyListPointStart, U32); //polyListPointStart
	WRITECHECK(polyListStringStart, U32); //polyListStringStart
	return true;
}

bool Interior::CoordBin::read(std::istream &stream, Version &version) {
	READCHECK(binStart, U32); //binStart
	READCHECK(binCount, U32); //binCount
	return true;
}

bool Interior::CoordBin::write(std::ostream &stream, Version version) const {
	WRITECHECK(binStart, U32); //binStart
	WRITECHECK(binCount, U32); //binCount
	return true;
}

bool Interior::TexMatrix::read(std::istream &stream, Version &version) {
	READCHECK(T, S32); //T
	READCHECK(N, S32); //N
	READCHECK(B, S32); //B
	return true;
}

bool Interior::TexMatrix::write(std::ostream &stream, Version version) const {
	WRITECHECK(T, S32); //T
	WRITECHECK(N, S32); //N
	WRITECHECK(B, S32); //B
	return true;
}

DIF_NAMESPACE_END
