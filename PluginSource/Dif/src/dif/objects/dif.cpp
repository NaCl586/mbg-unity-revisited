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

#include <dif/base/io.h>
#include <dif/objects/dif.h>

DIF_NAMESPACE

bool DIF::read(std::istream &stream, Version &version) {
	//http://rustycode.com/tutorials/DIF_File_Format_44_14.html
	// Someone give that guy all the cookies.

	//Must always be 44
	U32 difVersion;
	READCHECK(difVersion, U32); //version
	if (difVersion != 44) { //interiorResourceFileVersion
		return false;
	}
	version.dif.version = difVersion;

	//We don't export these (as we export MBG spec)
	U8 previewIncluded;
	READCHECK(previewIncluded, U8); //previewIncluded
	if (previewIncluded) {
		PNG previewBitmap;
		READCHECK(previewBitmap, PNG); //previewBitmap
	}

	READCHECK(interior, std::vector<Interior>); //interior
	READCHECK(subObject, std::vector<Interior>); //subObject
	READCHECK(trigger, std::vector<Trigger>); //trigger
	READCHECK(interiorPathFollower, std::vector<InteriorPathFollower>); //interiorPathFollower
	READCHECK(forceField, std::vector<ForceField>); //forceField
	READCHECK(aiSpecialNode, std::vector<AISpecialNode>); //aiSpecialNode

	U32 readVehicleCollision;
	READCHECK(readVehicleCollision, U32); //readVehicleCollision
	if (readVehicleCollision) {
		vehicleCollision.read(stream, version); //vehicleCollision
	}

	U32 readGameEntities;
	READCHECK(readGameEntities, U32); //readGameEntities
	if (readGameEntities == 2) {
		READCHECK(gameEntity, std::vector<GameEntity>); //gameEntity
	}

	//Makes no difference if we read this, and ignoring it stops the io from
	// exploding when it magically disappears from some DIF files
//	U32 dummy;
//	READCHECK(U32, dummy); //dummy

	if (version.dif.type == Version::DIFVersion::Type::Unknown) {
		version.dif.type = (version.interior.type == Version::InteriorVersion::Type::MBG ? Version::DIFVersion::Type::MBG : Version::DIFVersion::Type::TGE);
	}

	return true;
}

bool DIF::write(std::ostream &stream, Version version) const {
	WRITECHECK(version.dif.version, U32); //interiorResourceFileVersion
	WRITECHECK(0, U8); //previewIncluded

	WRITECHECK(interior, std::vector<Interior>); //interior
	WRITECHECK(subObject, std::vector<Interior>); //subObject
	WRITECHECK(trigger, std::vector<Trigger>); //trigger
	WRITECHECK(interiorPathFollower, std::vector<InteriorPathFollower>); //interiorPathFollower
	WRITECHECK(forceField, std::vector<ForceField>); //forceField
	WRITECHECK(aiSpecialNode, std::vector<AISpecialNode>); //aiSpecialNode
	WRITECHECK(1, U32); //readVehicleCollision
	vehicleCollision.write(stream, version); //vehicleCollision

	if (gameEntity.size()) {
		WRITECHECK(2, U32); //readGameEntities
		WRITECHECK(gameEntity, std::vector<GameEntity>); //gameEntity
	} else {
		WRITECHECK(0, U32); //readGameEntities
	}

	WRITECHECK(0, U32); //dummy

	return true;
}

DIF_NAMESPACE_END
