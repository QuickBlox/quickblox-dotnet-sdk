#include "pch.h"

#include "VideoCaptureTransform_h.h"
#include "BufferLock.h"

using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;
using namespace ABI::Windows::Foundation;
using namespace ABI::Windows::Foundation::Collections;
using namespace ABI::Windows::Storage::Streams;

template <class T> void SafeRelease(T **ppT)
{
	if (*ppT)
	{
		(*ppT)->Release();
		*ppT = nullptr;
	}
}

// Implements a capture video effect. 
class CCapture : public RuntimeClass<RuntimeClassFlags<RuntimeClassType::WinRtClassicComMix>, ABI::Windows::Media::IMediaExtension, IMFTransform>
{
	InspectableClass(RuntimeClass_VideoCaptureTransform_CaptureEffect, BaseTrust)

public:
	CCapture();

	STDMETHOD(RuntimeClassInitialize)();

	// IMediaExtension
	STDMETHODIMP SetProperties(ABI::Windows::Foundation::Collections::IPropertySet *pConfiguration);

	// IMFTransform
	STDMETHODIMP GetStreamLimits(
		DWORD   *pdwInputMinimum,
		DWORD   *pdwInputMaximum,
		DWORD   *pdwOutputMinimum,
		DWORD   *pdwOutputMaximum
		);

	STDMETHODIMP GetStreamCount(
		DWORD   *pcInputStreams,
		DWORD   *pcOutputStreams
		);

	STDMETHODIMP GetStreamIDs(
		DWORD   dwInputIDArraySize,
		DWORD   *pdwInputIDs,
		DWORD   dwOutputIDArraySize,
		DWORD   *pdwOutputIDs
		);

	STDMETHODIMP GetInputStreamInfo(
		DWORD                     dwInputStreamID,
		MFT_INPUT_STREAM_INFO *   pStreamInfo
		);

	STDMETHODIMP GetOutputStreamInfo(
		DWORD                     dwOutputStreamID,
		MFT_OUTPUT_STREAM_INFO *  pStreamInfo
		);

	STDMETHODIMP GetAttributes(IMFAttributes** pAttributes);

	STDMETHODIMP GetInputStreamAttributes(
		DWORD           dwInputStreamID,
		IMFAttributes   **ppAttributes
		);

	STDMETHODIMP GetOutputStreamAttributes(
		DWORD           dwOutputStreamID,
		IMFAttributes   **ppAttributes
		);

	STDMETHODIMP DeleteInputStream(DWORD dwStreamID);

	STDMETHODIMP AddInputStreams(
		DWORD   cStreams,
		DWORD   *adwStreamIDs
		);

	STDMETHODIMP GetInputAvailableType(
		DWORD           dwInputStreamID,
		DWORD           dwTypeIndex, // 0-based
		IMFMediaType    **ppType
		);

	STDMETHODIMP GetOutputAvailableType(
		DWORD           dwOutputStreamID,
		DWORD           dwTypeIndex, // 0-based
		IMFMediaType    **ppType
		);

	STDMETHODIMP SetInputType(
		DWORD           dwInputStreamID,
		IMFMediaType    *pType,
		DWORD           dwFlags
		);

	STDMETHODIMP SetOutputType(
		DWORD           dwOutputStreamID,
		IMFMediaType    *pType,
		DWORD           dwFlags
		);

	STDMETHODIMP GetInputCurrentType(
		DWORD           dwInputStreamID,
		IMFMediaType    **ppType
		);

	STDMETHODIMP GetOutputCurrentType(
		DWORD           dwOutputStreamID,
		IMFMediaType    **ppType
		);

	STDMETHODIMP GetInputStatus(
		DWORD           dwInputStreamID,
		DWORD           *pdwFlags
		);

	STDMETHODIMP GetOutputStatus(DWORD *pdwFlags);

	STDMETHODIMP SetOutputBounds(
		LONGLONG        hnsLowerBound,
		LONGLONG        hnsUpperBound
		);

	STDMETHODIMP ProcessEvent(
		DWORD              dwInputStreamID,
		IMFMediaEvent      *pEvent
		);

	STDMETHODIMP ProcessMessage(
		MFT_MESSAGE_TYPE    eMessage,
		ULONG_PTR           ulParam
		);

	STDMETHODIMP ProcessInput(
		DWORD               dwInputStreamID,
		IMFSample           *pSample,
		DWORD               dwFlags
		);

	STDMETHODIMP ProcessOutput(
		DWORD                   dwFlags,
		DWORD                   cOutputBufferCount,
		MFT_OUTPUT_DATA_BUFFER  *pOutputSamples, // one per stream
		DWORD                   *pdwStatus
		);

private:
	~CCapture();

	// HasPendingOutput: Returns TRUE if the MFT is holding an input sample.
	BOOL HasPendingOutput() const { return m_pSample != nullptr; }

	// IsValidInputStream: Returns TRUE if dwInputStreamID is a valid input stream identifier.
	BOOL IsValidInputStream(DWORD dwInputStreamID) const
	{
		return dwInputStreamID == 0;
	}

	// IsValidOutputStream: Returns TRUE if dwOutputStreamID is a valid output stream identifier.
	BOOL IsValidOutputStream(DWORD dwOutputStreamID) const
	{
		return dwOutputStreamID == 0;
	}

	HRESULT OnGetPartialType(DWORD dwTypeIndex, IMFMediaType **ppmt);
	HRESULT OnCheckInputType(IMFMediaType *pmt);
	HRESULT OnCheckOutputType(IMFMediaType *pmt);
	HRESULT OnCheckMediaType(IMFMediaType *pmt);
	void    OnSetInputType(IMFMediaType *pmt);
	void    OnSetOutputType(IMFMediaType *pmt);
	HRESULT BeginStreaming();
	HRESULT EndStreaming();
	HRESULT OnProcessOutput(IMFMediaBuffer *pIn, IMFMediaBuffer *pOut);
	HRESULT OnFlush();
	HRESULT UpdateFormatInfo();

	CRITICAL_SECTION            m_critSec;

	// Streaming 
	IMFSample                   *m_pSample;                 // Input sample.
	IMFMediaType                *m_pInputType;              // Input media type.
	IMFMediaType                *m_pOutputType;             // Output media type. 

	// Format information
	UINT32                      m_imageWidthInPixels;
	UINT32                      m_imageHeightInPixels;
	DWORD                       m_cbImageSize;              // Image size, in bytes.

	IMFAttributes               *m_pAttributes;

	// Settings
	ComPtr<IMap<HSTRING, IInspectable *>> m_pSettings;
};
ActivatableClass(CCapture);

#pragma comment(lib, "d2d1")

/*

This sample implements a video capture effect as a Media Foundation transform (MFT).


NOTES ON THE MFT IMPLEMENTATION

1. The MFT has fixed streams: One input stream and one output stream.

2. If the MFT is holding an input sample, SetInputType and SetOutputType both fail.

3. The input and output types must be identical.

4. If both types are set, no type can be set until the current type is cleared.

5. Preferred input types:

(a) If the output type is set, that's the preferred type.
(b) Otherwise, the preferred types are partial types, constructed from the
list of supported subtypes.

6. Preferred output types: As above.

7. Streaming:

The private BeingStreaming() method is called in response to the
MFT_MESSAGE_NOTIFY_BEGIN_STREAMING message.

If the client does not send MFT_MESSAGE_NOTIFY_BEGIN_STREAMING, the MFT calls
BeginStreaming inside the first call to ProcessInput or ProcessOutput.

This is a good approach for allocating resources that your MFT requires for
streaming.

8. The configuration attributes are applied in the BeginStreaming method. If the
client changes the attributes during streaming, the change is ignored until
streaming is stopped (either by changing the media types or by sending the
MFT_MESSAGE_NOTIFY_END_STREAMING message) and then restarted.

*/


// Video FOURCC codes. 
const DWORD FOURCC_YUY2 = '2YUY';
const DWORD FOURCC_UYVY = 'YVYU';
const DWORD FOURCC_NV12 = '21VN';

// Static array of media types (preferred and accepted). 
const GUID g_MediaSubtypes[] =
{
	MFVideoFormat_YUY2,
	MFVideoFormat_NV12,
	MFVideoFormat_UYVY
};

HRESULT GetImageSize(DWORD fcc, UINT32 width, UINT32 height, DWORD* pcbImage);
HRESULT GetDefaultStride(IMFMediaType *pType, LONG *plStride);


CCapture::CCapture() :
m_pSample(nullptr), m_pInputType(nullptr), m_pOutputType(nullptr),
m_imageWidthInPixels(0), m_imageHeightInPixels(0), m_cbImageSize(0), m_pAttributes(nullptr)
{
	InitializeCriticalSectionEx(&m_critSec, 3000, 0);
}

CCapture::~CCapture()
{
	SafeRelease(&m_pInputType);
	SafeRelease(&m_pOutputType);
	SafeRelease(&m_pSample);
	SafeRelease(&m_pAttributes);
	DeleteCriticalSection(&m_critSec);
}

// Initialize the instance.
STDMETHODIMP CCapture::RuntimeClassInitialize()
{
	// Create the attribute store. 
	return MFCreateAttributes(&m_pAttributes, 3);
}

// IMediaExtension methods 

//------------------------------------------------------------------- 
// SetProperties 
// Sets the configuration of the effect 
//-------------------------------------------------------------------
HRESULT CCapture::SetProperties(IPropertySet *pConfiguration)
{
	// get a usable map
	pConfiguration->QueryInterface(IID_PPV_ARGS(&m_pSettings));

	return S_OK;
}

// IMFTransform methods. Refer to the Media Foundation SDK documentation for details. 

//------------------------------------------------------------------- 
// GetStreamLimits 
// Returns the minimum and maximum number of streams. 
//-------------------------------------------------------------------

HRESULT CCapture::GetStreamLimits(
	DWORD   *pdwInputMinimum,
	DWORD   *pdwInputMaximum,
	DWORD   *pdwOutputMinimum,
	DWORD   *pdwOutputMaximum
	)
{
	// This MFT has a fixed number of streams.
	*pdwInputMinimum = 1;
	*pdwInputMaximum = 1;
	*pdwOutputMinimum = 1;
	*pdwOutputMaximum = 1;
	return S_OK;
}

// Returns the actual number of streams.

HRESULT CCapture::GetStreamCount(
	DWORD   *pcInputStreams,
	DWORD   *pcOutputStreams
	)
{
	// This MFT has a fixed number of streams.
	*pcInputStreams = 1;
	*pcOutputStreams = 1;
	return S_OK;
}



//------------------------------------------------------------------- 
// GetStreamIDs 
// Returns stream IDs for the input and output streams. 
//-------------------------------------------------------------------

HRESULT CCapture::GetStreamIDs(
	DWORD   dwInputIDArraySize,
	DWORD   *pdwInputIDs,
	DWORD   dwOutputIDArraySize,
	DWORD   *pdwOutputIDs
	)
{
	// It is not required to implement this method if the MFT has a fixed number of 
	// streams AND the stream IDs are numbered sequentially from zero (that is, the 
	// stream IDs match the stream indexes). 

	// In that case, it is OK to return E_NOTIMPL. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// GetInputStreamInfo 
// Returns information about an input stream. 
//-------------------------------------------------------------------

HRESULT CCapture::GetInputStreamInfo(
	DWORD                     dwInputStreamID,
	MFT_INPUT_STREAM_INFO *   pStreamInfo
	)
{
	EnterCriticalSection(&m_critSec);

	if (!IsValidInputStream(dwInputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	// NOTE: This method should succeed even when there is no media type on the 
	//       stream. If there is no media type, we only need to fill in the dwFlags 
	//       member of MFT_INPUT_STREAM_INFO. The other members depend on having a 
	//       a valid media type.

	pStreamInfo->hnsMaxLatency = 0;
	pStreamInfo->dwFlags = MFT_INPUT_STREAM_WHOLE_SAMPLES | MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER;

	if (m_pInputType == nullptr)
	{
		pStreamInfo->cbSize = 0;
	}
	else
	{
		pStreamInfo->cbSize = m_cbImageSize;
	}

	pStreamInfo->cbMaxLookahead = 0;
	pStreamInfo->cbAlignment = 0;

	LeaveCriticalSection(&m_critSec);
	return S_OK;
}

//------------------------------------------------------------------- 
// GetOutputStreamInfo 
// Returns information about an output stream. 
//-------------------------------------------------------------------

HRESULT CCapture::GetOutputStreamInfo(
	DWORD                     dwOutputStreamID,
	MFT_OUTPUT_STREAM_INFO *  pStreamInfo
	)
{
	EnterCriticalSection(&m_critSec);

	if (!IsValidOutputStream(dwOutputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	// NOTE: This method should succeed even when there is no media type on the 
	//       stream. If there is no media type, we only need to fill in the dwFlags 
	//       member of MFT_OUTPUT_STREAM_INFO. The other members depend on having a 
	//       a valid media type.

	pStreamInfo->dwFlags =
		MFT_OUTPUT_STREAM_WHOLE_SAMPLES |
		MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER |
		MFT_OUTPUT_STREAM_FIXED_SAMPLE_SIZE;

	if (m_pOutputType == nullptr)
	{
		pStreamInfo->cbSize = 0;
	}
	else
	{
		pStreamInfo->cbSize = m_cbImageSize;
	}

	pStreamInfo->cbAlignment = 0;

	LeaveCriticalSection(&m_critSec);
	return S_OK;
}

// Returns the attributes for the MFT.
HRESULT CCapture::GetAttributes(IMFAttributes** ppAttributes)
{
	EnterCriticalSection(&m_critSec);

	*ppAttributes = m_pAttributes;
	(*ppAttributes)->AddRef();

	LeaveCriticalSection(&m_critSec);
	return S_OK;
}

// Returns stream-level attributes for an input stream.

HRESULT CCapture::GetInputStreamAttributes(
	DWORD           dwInputStreamID,
	IMFAttributes   **ppAttributes
	)
{
	// This MFT does not support any stream-level attributes, so the method is not implemented. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// GetOutputStreamAttributes 
// Returns stream-level attributes for an output stream. 
//-------------------------------------------------------------------

HRESULT CCapture::GetOutputStreamAttributes(
	DWORD           dwOutputStreamID,
	IMFAttributes   **ppAttributes
	)
{
	// This MFT does not support any stream-level attributes, so the method is not implemented. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// DeleteInputStream 
//-------------------------------------------------------------------

HRESULT CCapture::DeleteInputStream(DWORD dwStreamID)
{
	// This MFT has a fixed number of input streams, so the method is not supported. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// AddInputStreams 
//-------------------------------------------------------------------

HRESULT CCapture::AddInputStreams(
	DWORD   cStreams,
	DWORD   *adwStreamIDs
	)
{
	// This MFT has a fixed number of output streams, so the method is not supported. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// GetInputAvailableType 
// Returns a preferred input type. 
//-------------------------------------------------------------------

HRESULT CCapture::GetInputAvailableType(
	DWORD           dwInputStreamID,
	DWORD           dwTypeIndex, // 0-based
	IMFMediaType    **ppType
	)
{
	EnterCriticalSection(&m_critSec);

	if (!IsValidInputStream(dwInputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	HRESULT hr = S_OK;

	// If the output type is set, return that type as our preferred input type. 
	if (m_pOutputType == nullptr)
	{
		// The output type is not set. Create a partial media type.
		hr = OnGetPartialType(dwTypeIndex, ppType);
	}
	else if (dwTypeIndex > 0)
	{
		hr = MF_E_NO_MORE_TYPES;
	}
	else
	{
		*ppType = m_pOutputType;
		(*ppType)->AddRef();
	}

	LeaveCriticalSection(&m_critSec);
	return hr;
}

// Returns a preferred output type.

HRESULT CCapture::GetOutputAvailableType(
	DWORD           dwOutputStreamID,
	DWORD           dwTypeIndex, // 0-based
	IMFMediaType    **ppType
	)
{
	EnterCriticalSection(&m_critSec);

	if (!IsValidOutputStream(dwOutputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	HRESULT hr = S_OK;

	if (m_pInputType == nullptr)
	{
		// The input type is not set. Create a partial media type.
		hr = OnGetPartialType(dwTypeIndex, ppType);
	}
	else if (dwTypeIndex > 0)
	{
		hr = MF_E_NO_MORE_TYPES;
	}
	else
	{
		*ppType = m_pInputType;
		(*ppType)->AddRef();
	}

	LeaveCriticalSection(&m_critSec);
	return hr;
}

HRESULT CCapture::SetInputType(
	DWORD           dwInputStreamID,
	IMFMediaType    *pType, // Can be nullptr to clear the input type.
	DWORD           dwFlags
	)
{
	// Validate flags. 
	if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY)
	{
		return E_INVALIDARG;
	}

	EnterCriticalSection(&m_critSec);

	if (!IsValidInputStream(dwInputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	HRESULT hr = S_OK;

	// Does the caller want us to set the type, or just test it?
	BOOL bReallySet = ((dwFlags & MFT_SET_TYPE_TEST_ONLY) == 0);

	// If we have an input sample, the client cannot change the type now. 
	if (HasPendingOutput())
	{
		hr = MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
		goto done;
	}

	// Validate the type, if non-nullptr. 
	if (pType)
	{
		hr = OnCheckInputType(pType);
		if (FAILED(hr))
		{
			goto done;
		}
	}

	// The type is OK. Set the type, unless the caller was just testing. 
	if (bReallySet)
	{
		OnSetInputType(pType);

		// When the type changes, end streaming.
		hr = EndStreaming();
	}

done:
	LeaveCriticalSection(&m_critSec);
	return hr;
}

HRESULT CCapture::SetOutputType(
	DWORD           dwOutputStreamID,
	IMFMediaType    *pType, // Can be nullptr to clear the output type.
	DWORD           dwFlags
	)
{
	// Validate flags. 
	if (dwFlags & ~MFT_SET_TYPE_TEST_ONLY)
	{
		return E_INVALIDARG;
	}

	EnterCriticalSection(&m_critSec);

	if (!IsValidOutputStream(dwOutputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	HRESULT hr = S_OK;

	// Does the caller want us to set the type, or just test it?
	BOOL bReallySet = ((dwFlags & MFT_SET_TYPE_TEST_ONLY) == 0);

	// If we have an input sample, the client cannot change the type now. 
	if (HasPendingOutput())
	{
		hr = MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
		goto done;
	}

	// Validate the type, if non-nullptr. 
	if (pType)
	{
		hr = OnCheckOutputType(pType);
		if (FAILED(hr))
		{
			goto done;
		}
	}

	// The type is OK. Set the type, unless the caller was just testing. 
	if (bReallySet)
	{
		OnSetOutputType(pType);

		// When the type changes, end streaming.
		hr = EndStreaming();
	}

done:
	LeaveCriticalSection(&m_critSec);
	return hr;
}

// Returns the current input type.

HRESULT CCapture::GetInputCurrentType(
	DWORD           dwInputStreamID,
	IMFMediaType    **ppType
	)
{
	HRESULT hr = S_OK;

	EnterCriticalSection(&m_critSec);

	if (!IsValidInputStream(dwInputStreamID))
	{
		hr = MF_E_INVALIDSTREAMNUMBER;
	}
	else if (!m_pInputType)
	{
		hr = MF_E_TRANSFORM_TYPE_NOT_SET;
	}
	else
	{
		*ppType = m_pInputType;
		(*ppType)->AddRef();
	}
	LeaveCriticalSection(&m_critSec);
	return hr;
}

// Returns the current output type.

HRESULT CCapture::GetOutputCurrentType(
	DWORD           dwOutputStreamID,
	IMFMediaType    **ppType
	)
{
	HRESULT hr = S_OK;

	EnterCriticalSection(&m_critSec);

	if (!IsValidOutputStream(dwOutputStreamID))
	{
		hr = MF_E_INVALIDSTREAMNUMBER;
	}
	else if (!m_pOutputType)
	{
		hr = MF_E_TRANSFORM_TYPE_NOT_SET;
	}
	else
	{
		*ppType = m_pOutputType;
		(*ppType)->AddRef();
	}

	LeaveCriticalSection(&m_critSec);
	return hr;
}

// Query if the MFT is accepting more input.

HRESULT CCapture::GetInputStatus(
	DWORD           dwInputStreamID,
	DWORD           *pdwFlags
	)
{
	EnterCriticalSection(&m_critSec);

	if (!IsValidInputStream(dwInputStreamID))
	{
		LeaveCriticalSection(&m_critSec);
		return MF_E_INVALIDSTREAMNUMBER;
	}

	// If an input sample is already queued, do not accept another sample until the  
	// client calls ProcessOutput or Flush. 

	// NOTE: It is possible for an MFT to accept more than one input sample. For  
	// example, this might be required in a video decoder if the frames do not  
	// arrive in temporal order. In the case, the decoder must hold a queue of  
	// samples. For the video effect, each sample is transformed independently, so 
	// there is no reason to queue multiple input samples. 

	if (m_pSample == nullptr)
	{
		*pdwFlags = MFT_INPUT_STATUS_ACCEPT_DATA;
	}
	else
	{
		*pdwFlags = 0;
	}

	LeaveCriticalSection(&m_critSec);
	return S_OK;
}

// Query if the MFT can produce output.

HRESULT CCapture::GetOutputStatus(DWORD *pdwFlags)
{
	EnterCriticalSection(&m_critSec);

	// The MFT can produce an output sample if (and only if) there an input sample. 
	if (m_pSample != nullptr)
	{
		*pdwFlags = MFT_OUTPUT_STATUS_SAMPLE_READY;
	}
	else
	{
		*pdwFlags = 0;
	}

	LeaveCriticalSection(&m_critSec);
	return S_OK;
}


//------------------------------------------------------------------- 
// SetOutputBounds 
// Sets the range of time stamps that the MFT will output. 
//-------------------------------------------------------------------

HRESULT CCapture::SetOutputBounds(
	LONGLONG        hnsLowerBound,
	LONGLONG        hnsUpperBound
	)
{
	// Implementation of this method is optional. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// ProcessEvent 
// Sends an event to an input stream. 
//-------------------------------------------------------------------

HRESULT CCapture::ProcessEvent(
	DWORD              dwInputStreamID,
	IMFMediaEvent      *pEvent
	)
{
	// This MFT does not handle any stream events, so the method can 
	// return E_NOTIMPL. This tells the pipeline that it can stop 
	// sending any more events to this MFT. 
	return E_NOTIMPL;
}


//------------------------------------------------------------------- 
// ProcessMessage 
//-------------------------------------------------------------------

HRESULT CCapture::ProcessMessage(
	MFT_MESSAGE_TYPE    eMessage,
	ULONG_PTR           ulParam
	)
{
	EnterCriticalSection(&m_critSec);

	HRESULT hr = S_OK;

	switch (eMessage)
	{
	case MFT_MESSAGE_COMMAND_FLUSH:
		// Flush the MFT.
		hr = OnFlush();
		break;

	case MFT_MESSAGE_COMMAND_DRAIN:
		// Drain: Tells the MFT to reject further input until all pending samples are 
		// processed. That is our default behavior already, so there is nothing to do. 
		// 
		// For a decoder that accepts a queue of samples, the MFT might need to drain 
		// the queue in response to this command. 
		break;

	case MFT_MESSAGE_SET_D3D_MANAGER:
		// Sets a pointer to the IDirect3DDeviceManager9 interface. 

		// The pipeline should never send this message unless the MFT sets the MF_SA_D3D_AWARE  
		// attribute set to TRUE. Because this MFT does not set MF_SA_D3D_AWARE, it is an error 
		// to send the MFT_MESSAGE_SET_D3D_MANAGER message to the MFT. Return an error code in 
		// this case. 

		// NOTE: If this MFT were D3D-enabled, it would cache the IDirect3DDeviceManager9  
		// pointer for use during streaming.

		hr = E_NOTIMPL;
		break;

	case MFT_MESSAGE_NOTIFY_BEGIN_STREAMING:
		hr = BeginStreaming();
		break;

	case MFT_MESSAGE_NOTIFY_END_STREAMING:
		hr = EndStreaming();
		break;

		// The next two messages do not require any action from this MFT. 

	case MFT_MESSAGE_NOTIFY_END_OF_STREAM:
		break;

	case MFT_MESSAGE_NOTIFY_START_OF_STREAM:
		break;
	}

	LeaveCriticalSection(&m_critSec);
	return hr;
}

// Process an input sample.

HRESULT CCapture::ProcessInput(
	DWORD               dwInputStreamID,
	IMFSample           *pSample,
	DWORD               dwFlags
	)
{
	if (dwFlags != 0)
	{
		return E_INVALIDARG; // dwFlags is reserved and must be zero.
	}

	HRESULT hr = S_OK;

	EnterCriticalSection(&m_critSec);

	// Validate the input stream number. 
	if (!IsValidInputStream(dwInputStreamID))
	{
		hr = MF_E_INVALIDSTREAMNUMBER;
		goto done;
	}

	// Check for valid media types. 
	// The client must set input and output types before calling ProcessInput. 
	if (!m_pInputType || !m_pOutputType)
	{
		hr = MF_E_NOTACCEPTING;
		goto done;
	}

	// Check if an input sample is already queued. 
	if (m_pSample != nullptr)
	{
		hr = MF_E_NOTACCEPTING;   // We already have an input sample. 
		goto done;
	}

	// Initialize streaming.
	hr = BeginStreaming();
	if (FAILED(hr))
	{
		goto done;
	}

	// Cache the sample. We do the actual work in ProcessOutput.
	m_pSample = pSample;
	pSample->AddRef();  // Hold a reference count on the sample.

done:
	LeaveCriticalSection(&m_critSec);
	return hr;
}


//------------------------------------------------------------------- 
// ProcessOutput 
// Process an output sample. 
//-------------------------------------------------------------------

HRESULT CCapture::ProcessOutput(
	DWORD                   dwFlags,
	DWORD                   cOutputBufferCount,
	MFT_OUTPUT_DATA_BUFFER  *pOutputSamples, // one per stream
	DWORD                   *pdwStatus
	)
{
	// Check input parameters... 

	// This MFT does not accept any flags for the dwFlags parameter. 

	// The only defined flag is MFT_PROCESS_OUTPUT_DISCARD_WHEN_NO_BUFFER. This flag  
	// applies only when the MFT marks an output stream as lazy or optional. But this 
	// MFT has no lazy or optional streams, so the flag is not valid. 

	if (dwFlags != 0)
	{
		return E_INVALIDARG;
	}

	// There must be exactly one output buffer. 
	if (cOutputBufferCount != 1)
	{
		return E_INVALIDARG;
	}

	// It must contain a sample. 
	if (pOutputSamples[0].pSample == nullptr)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = S_OK;

	IMFMediaBuffer *pInput = nullptr;
	IMFMediaBuffer *pOutput = nullptr;

	EnterCriticalSection(&m_critSec);

	// There must be an input sample available for processing. 
	if (m_pSample == nullptr)
	{
		hr = MF_E_TRANSFORM_NEED_MORE_INPUT;
		goto done;
	}

	// Initialize streaming.

	hr = BeginStreaming();
	if (FAILED(hr))
	{
		goto done;
	}

	// Get the input buffer.
	hr = m_pSample->ConvertToContiguousBuffer(&pInput);
	if (FAILED(hr))
	{
		goto done;
	}

	// Get the output buffer.
	hr = pOutputSamples[0].pSample->ConvertToContiguousBuffer(&pOutput);
	if (FAILED(hr))
	{
		goto done;
	}

	hr = OnProcessOutput(pInput, pOutput);
	if (FAILED(hr))
	{
		goto done;
	}

	// Set status flags.
	pOutputSamples[0].dwStatus = 0;
	*pdwStatus = 0;


	// Copy the duration and time stamp from the input sample, if present.

	LONGLONG hnsDuration = 0;
	LONGLONG hnsTime = 0;

	if (SUCCEEDED(m_pSample->GetSampleDuration(&hnsDuration)))
	{
		hr = pOutputSamples[0].pSample->SetSampleDuration(hnsDuration);
		if (FAILED(hr))
		{
			goto done;
		}
	}

	if (SUCCEEDED(m_pSample->GetSampleTime(&hnsTime)))
	{
		hr = pOutputSamples[0].pSample->SetSampleTime(hnsTime);
	}

done:
	SafeRelease(&m_pSample);   // Release our input sample.
	SafeRelease(&pInput);
	SafeRelease(&pOutput);
	LeaveCriticalSection(&m_critSec);
	return hr;
}

// PRIVATE METHODS 

// All methods that follow are private to this MFT and are not part of the IMFTransform interface. 

// Create a partial media type from our list. 
// 
// dwTypeIndex: Index into the list of peferred media types. 
// ppmt:        Receives a pointer to the media type.

HRESULT CCapture::OnGetPartialType(DWORD dwTypeIndex, IMFMediaType **ppmt)
{
	if (dwTypeIndex >= ARRAYSIZE(g_MediaSubtypes))
	{
		return MF_E_NO_MORE_TYPES;
	}

	IMFMediaType *pmt = nullptr;

	HRESULT hr = MFCreateMediaType(&pmt);
	if (FAILED(hr))
	{
		goto done;
	}

	hr = pmt->SetGUID(MF_MT_MAJOR_TYPE, MFMediaType_Video);
	if (FAILED(hr))
	{
		goto done;
	}

	hr = pmt->SetGUID(MF_MT_SUBTYPE, g_MediaSubtypes[dwTypeIndex]);
	if (FAILED(hr))
	{
		goto done;
	}

	*ppmt = pmt;
	(*ppmt)->AddRef();

done:
	SafeRelease(&pmt);
	return hr;
}

// Validate an input media type.

HRESULT CCapture::OnCheckInputType(IMFMediaType *pmt)
{
	assert(pmt != nullptr);

	HRESULT hr = S_OK;

	// If the output type is set, see if they match. 
	if (m_pOutputType != nullptr)
	{
		DWORD flags = 0;
		hr = pmt->IsEqual(m_pOutputType, &flags);

		// IsEqual can return S_FALSE. Treat this as failure. 
		if (hr != S_OK)
		{
			hr = MF_E_INVALIDMEDIATYPE;
		}
	}
	else
	{
		// Output type is not set. Just check this type.
		hr = OnCheckMediaType(pmt);
	}
	return hr;
}

// Validate an output media type.

HRESULT CCapture::OnCheckOutputType(IMFMediaType *pmt)
{
	assert(pmt != nullptr);

	HRESULT hr = S_OK;

	// If the input type is set, see if they match. 
	if (m_pInputType != nullptr)
	{
		DWORD flags = 0;
		hr = pmt->IsEqual(m_pInputType, &flags);

		// IsEqual can return S_FALSE. Treat this as failure. 
		if (hr != S_OK)
		{
			hr = MF_E_INVALIDMEDIATYPE;
		}

	}
	else
	{
		// Input type is not set. Just check this type.
		hr = OnCheckMediaType(pmt);
	}
	return hr;
}


// Validate a media type (input or output)

HRESULT CCapture::OnCheckMediaType(IMFMediaType *pmt)
{
	BOOL bFoundMatchingSubtype = FALSE;

	// Major type must be video.
	GUID major_type;
	HRESULT hr = pmt->GetGUID(MF_MT_MAJOR_TYPE, &major_type);
	if (FAILED(hr))
	{
		goto done;
	}

	if (major_type != MFMediaType_Video)
	{
		hr = MF_E_INVALIDMEDIATYPE;
		goto done;
	}

	// Subtype must be one of the subtypes in our global list. 

	// Get the subtype GUID.
	GUID subtype;
	hr = pmt->GetGUID(MF_MT_SUBTYPE, &subtype);
	if (FAILED(hr))
	{
		goto done;
	}

	// Look for the subtype in our list of accepted types. 
	for (DWORD i = 0; i < ARRAYSIZE(g_MediaSubtypes); i++)
	{
		if (subtype == g_MediaSubtypes[i])
		{
			bFoundMatchingSubtype = TRUE;
			break;
		}
	}

	if (!bFoundMatchingSubtype)
	{
		hr = MF_E_INVALIDMEDIATYPE; // The MFT does not support this subtype. 
		goto done;
	}

	// Reject single-field media types. 
	UINT32 interlace = MFGetAttributeUINT32(pmt, MF_MT_INTERLACE_MODE, MFVideoInterlace_Progressive);
	if (interlace == MFVideoInterlace_FieldSingleUpper || interlace == MFVideoInterlace_FieldSingleLower)
	{
		hr = MF_E_INVALIDMEDIATYPE;
	}

done:
	return hr;
}


// Set or clear the input media type. 
// 
// Prerequisite: The input type was already validated. 

void CCapture::OnSetInputType(IMFMediaType *pmt)
{
	// if pmt is nullptr, clear the type. 
	// if pmt is non-nullptr, set the type.

	SafeRelease(&m_pInputType);
	m_pInputType = pmt;
	if (m_pInputType)
	{
		m_pInputType->AddRef();
	}

	// Update the format information.
	UpdateFormatInfo();
}


// Set or clears the output media type. 
// 
// Prerequisite: The output type was already validated. 

void CCapture::OnSetOutputType(IMFMediaType *pmt)
{
	// If pmt is nullptr, clear the type. Otherwise, set the type.

	SafeRelease(&m_pOutputType);
	m_pOutputType = pmt;
	if (m_pOutputType)
	{
		m_pOutputType->AddRef();
	}
}


// Initialize streaming parameters. 
// 
// This method is called if the client sends the MFT_MESSAGE_NOTIFY_BEGIN_STREAMING 
// message, or when the client processes a sample, whichever happens first.

HRESULT CCapture::BeginStreaming()
{
	return S_OK;
}


// End streaming.  

// This method is called if the client sends an MFT_MESSAGE_NOTIFY_END_STREAMING 
// message, or when the media type changes. In general, it should be called whenever 
// the streaming parameters need to be reset.

HRESULT CCapture::EndStreaming()
{
	return S_OK;
}



// Generate output data.

HRESULT CCapture::OnProcessOutput(IMFMediaBuffer *pIn, IMFMediaBuffer *pOut)
{
	BYTE *pDest = nullptr;         // Destination buffer.
	LONG lDestStride = 0;       // Destination stride.

	BYTE *pSrc = nullptr;          // Source buffer.
	LONG lSrcStride = 0;        // Source stride. 

	ComPtr<IPropertyValueStatics> propValueFactory = nullptr;
	ComPtr<IInspectable> widthInspectable = nullptr;
	ComPtr<IInspectable> heightInspectable = nullptr;
	ComPtr<IInspectable> strideInspectable = nullptr;
	ComPtr<IInspectable> dataInspectable = nullptr;

	// Helper objects to lock the buffers.
	VideoBufferLock inputLock(pIn);
	VideoBufferLock outputLock(pOut);

	// Stride if the buffer does not support IMF2DBuffer
	LONG lDefaultStride = 0;

	HRESULT hr = GetDefaultStride(m_pInputType, &lDefaultStride);
	if (FAILED(hr))
	{
		goto done;
	}

	// Lock the input buffer.
	hr = inputLock.LockBuffer(lDefaultStride, m_imageHeightInPixels, &pSrc, &lSrcStride);
	if (FAILED(hr))
	{
		goto done;
	}

	// Lock the output buffer.
	hr = outputLock.LockBuffer(lDefaultStride, m_imageHeightInPixels, &pDest, &lDestStride);
	if (FAILED(hr))
	{
		goto done;
	}

	// Get prop factory.
	GetActivationFactory(HStringReference(RuntimeClass_Windows_Foundation_PropertyValue).Get(), &propValueFactory);

	// Move frame to props.
	propValueFactory->CreateInt32(m_imageWidthInPixels, &widthInspectable);
	propValueFactory->CreateInt32(m_imageHeightInPixels, &heightInspectable);
	propValueFactory->CreateInt32(lSrcStride, &strideInspectable);
	propValueFactory->CreateUInt8Array(lSrcStride * m_imageHeightInPixels, pSrc, &dataInspectable);

	boolean replaced;
	m_pSettings->Insert(HStringReference(L"videoWidth").Get(), widthInspectable.Get(), &replaced);
	m_pSettings->Insert(HStringReference(L"videoHeight").Get(), heightInspectable.Get(), &replaced);
	m_pSettings->Insert(HStringReference(L"videoStride").Get(), heightInspectable.Get(), &replaced);
	m_pSettings->Insert(HStringReference(L"videoData").Get(), dataInspectable.Get(), &replaced);

	memcpy(pDest, pSrc, lSrcStride * m_imageHeightInPixels);

	// Set the data size on the output buffer.
	hr = pOut->SetCurrentLength(m_cbImageSize);

	// The VideoBufferLock class automatically unlocks the buffers.
done:
	return hr;
}


// Flush the MFT.

HRESULT CCapture::OnFlush()
{
	// For this MFT, flushing just means releasing the input sample.
	SafeRelease(&m_pSample);
	return S_OK;
}

// Update the format information. This method is called whenever the 
// input type is set.

HRESULT CCapture::UpdateFormatInfo()
{
	HRESULT hr = S_OK;

	GUID subtype = GUID_NULL;

	m_imageWidthInPixels = 0;
	m_imageHeightInPixels = 0;
	m_cbImageSize = 0;

	if (m_pInputType != nullptr)
	{
		hr = m_pInputType->GetGUID(MF_MT_SUBTYPE, &subtype);
		if (FAILED(hr))
		{
			goto done;
		}
		if (subtype == MFVideoFormat_YUY2)
		{ }
		else if (subtype == MFVideoFormat_UYVY)
		{ }
		else if (subtype == MFVideoFormat_NV12)
		{ }
		else
		{
			hr = E_UNEXPECTED;
			goto done;
		}

		hr = MFGetAttributeSize(m_pInputType, MF_MT_FRAME_SIZE, &m_imageWidthInPixels, &m_imageHeightInPixels);
		if (FAILED(hr))
		{
			goto done;
		}

		// Calculate the image size (not including padding)
		hr = GetImageSize(subtype.Data1, m_imageWidthInPixels, m_imageHeightInPixels, &m_cbImageSize);
	}

done:
	return hr;
}


// Calculate the size of the buffer needed to store the image. 

// fcc: The FOURCC code of the video format.

HRESULT GetImageSize(DWORD fcc, UINT32 width, UINT32 height, DWORD* pcbImage)
{
	HRESULT hr = S_OK;

	switch (fcc)
	{
	case FOURCC_YUY2:
	case FOURCC_UYVY:
		// check overflow 
		if ((width > MAXDWORD / 2) || (width * 2 > MAXDWORD / height))
		{
			hr = E_INVALIDARG;
		}
		else
		{
			// 16 bpp
			*pcbImage = width * height * 2;
		}
		break;

	case FOURCC_NV12:
		// check overflow 
		if ((height / 2 > MAXDWORD - height) || ((height + height / 2) > MAXDWORD / width))
		{
			hr = E_INVALIDARG;
		}
		else
		{
			// 12 bpp
			*pcbImage = width * (height + (height / 2));
		}
		break;

	default:
		hr = E_FAIL;    // Unsupported type.
	}
	return hr;
}

// Get the default stride for a video format. 
HRESULT GetDefaultStride(IMFMediaType *pType, LONG *plStride)
{
	LONG lStride = 0;

	// Try to get the default stride from the media type.
	HRESULT hr = pType->GetUINT32(MF_MT_DEFAULT_STRIDE, (UINT32*)&lStride);
	if (FAILED(hr))
	{
		// Attribute not set. Try to calculate the default stride.
		GUID subtype = GUID_NULL;

		UINT32 width = 0;
		UINT32 height = 0;

		// Get the subtype and the image size.
		hr = pType->GetGUID(MF_MT_SUBTYPE, &subtype);
		if (SUCCEEDED(hr))
		{
			hr = MFGetAttributeSize(pType, MF_MT_FRAME_SIZE, &width, &height);
		}
		if (SUCCEEDED(hr))
		{
			if (subtype == MFVideoFormat_NV12)
			{
				lStride = width;
			}
			else if (subtype == MFVideoFormat_YUY2 || subtype == MFVideoFormat_UYVY)
			{
				lStride = ((width * 2) + 3) & ~3;
			}
			else
			{
				hr = E_INVALIDARG;
			}
		}

		// Set the attribute for later reference. 
		if (SUCCEEDED(hr))
		{
			(void)pType->SetUINT32(MF_MT_DEFAULT_STRIDE, UINT32(lStride));
		}
	}
	if (SUCCEEDED(hr))
	{
		*plStride = lStride;
	}
	return hr;
}