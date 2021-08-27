// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <string>
#include <cstddef>
#include <base64.h>
#include <rsa.h>
#include <osrng.h>
#include <hex.h>
#include <files.h>
#include <pssr.h>

using namespace CryptoPP;
void RSAEncryptInit();

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
		RSAEncryptInit();
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

void decodePrivKeyBase64(std::string privKey);

void RSAEncryptInit()
{
	std::string privKey = "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";

	decodePrivKeyBase64(privKey);
}

std::string privKeyDecoded;

void decodePrivKeyBase64(std::string privKey)
{
	Base64Decoder decoder;
	decoder.Put((byte*)privKey.data(), privKey.size());
	decoder.MessageEnd();

	word64 size = decoder.MaxRetrievable();
	if (size && size <= SIZE_MAX)
	{
		privKeyDecoded.resize(size);
		decoder.Get((byte*)&privKeyDecoded[0], privKeyDecoded.size());
	}
}

std::vector<unsigned char> Encrypt(std::vector<unsigned char> input, int* sigLen)
{
	try {
		AutoSeededRandomPool rng;

		RSA::PrivateKey privateKey;
		privateKey.Load(StringSource(privKeyDecoded, true).Ref());

		RSASS<PSSR, SHA256>::Signer signer(privateKey);

		size_t messageLen = input.size();

		SecByteBlock signature(signer.MaxSignatureLength(messageLen));
		size_t signatureLen = signer.SignMessageWithRecovery(rng, reinterpret_cast<const byte*>(&input[0]), messageLen, NULL, 0, signature);

		signature.resize(signatureLen);
		*sigLen = signature.size();
		std::vector<unsigned char> cipher;
		cipher.resize(signature.size());
		std::memcpy(&cipher[0], &signature[0], signature.size());

		return cipher;
	}
	catch (CryptoPP::Exception& e) {
		return std::vector<unsigned char>();
	}
}


static std::vector<unsigned char> StringToByteArray(unsigned char* str, int len)
{
	std::vector<unsigned char> arr;
	for (int i = 0; i < len; i++)
		arr.push_back(str[i]);

	return arr;
}

extern "C" __declspec(dllexport) unsigned char* RSAEncrypt(unsigned char* in, int len, int* outLen, int* outSize)
{
	auto stuff = Encrypt(StringToByteArray(in, len), outLen);
	unsigned char* final_array = new unsigned char[stuff.size()];
	for (int i = 0; i < stuff.size(); i++)
		final_array[i] = stuff[i];
	*outSize = stuff.size();
	return final_array;
}