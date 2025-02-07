use super::bilge_util::Fix;
use bilge::prelude::*;

//type FP30 = fixed::types::I2F30;
pub(crate) type FP20 = fixed::types::I12F20;
pub(crate) type FP15 = fixed::types::I17F15;
pub(crate) type FP12 = fixed::types::I20F12;

#[bitsize(2)]
#[derive(FromBits, Copy, Clone, Debug)]
pub(crate) enum QuaternionMode {
    Individual,
    FirstLastDeltaMid,
    LastDeltaFirstDeltaMid,
    Unknown,
}

#[bitsize(123)]
#[derive(FromBits, DebugBits)]
pub(crate) struct Individual(pub [IndividualCompress; 3]);

#[bitsize(41)]
#[derive(FromBits, Copy, Clone, DebugBits)]
pub(crate) struct IndividualCompress {
    missing_idx: u2,
    components: [Fix::<u13, FP12>; 3],
}

impl IndividualCompress {
    pub fn unpack(self) -> (u2, crate::FP3<FP12>) {
        (
            self.missing_idx(),
            crate::FP3 {
                value: self.components().map(|e| e.0),
            },
        )
    }
}

// workaround for missing primitive impls
type B16 = UInt<u16, 16>;
type B8 = UInt<u8, 8>;

#[bitsize(123)]
#[derive(FromBits, DebugBits)]
pub(crate) struct FirstLastDeltaMid {
    /// indicates if mid_avg_delta was shifted down by an additional 2 bits and needs to be shifted back up
    additional_shift: bool,
    missing_idx: u2,
    first: [Fix::<B16, FP15>; 3],
    last: [Fix::<B16, FP15>; 3],
    mid_avg_delta: [Fix::<B8, FP15>; 3],
}

impl FirstLastDeltaMid {
    pub fn unpack(self) -> (u2, [crate::FP3<FP15>; 3]) {
        let first = crate::FP3::from(self.first());
        let last = crate::FP3::from(self.last());
        let avg = last.avg(&first);
        let mut mid_avg_delta = crate::FP3::from(self.mid_avg_delta());
        if self.additional_shift() {
            for elem in &mut mid_avg_delta.value {
                *elem <<= 2;
            }
        }

        // mid_avg_delta = mid - avg
        // mid_avg_delta + avg = mid
        let mid = avg + mid_avg_delta;

        (self.missing_idx(), [first, mid, last])
    }
}

#[bitsize(125)]
#[derive(FromBits, DebugBits)]
pub(crate) struct LastDeltaFirstDeltaMid {
    missing_idx: u2,
    last: [Fix::<u21, FP20>; 3],
    first_delta: [Fix::<u13, FP20>; 3],
    mid_avg_delta: [Fix::<u7, FP20>; 3],
}

impl LastDeltaFirstDeltaMid {
    pub fn unpack(self) -> (u2, [crate::FP3<FP20>; 3]) {
        let last = crate::FP3::from(self.last());
        let first_delta = crate::FP3::from(self.first_delta());
        let mid_avg_delta = crate::FP3::from(self.mid_avg_delta());

        println!(
            "last: {:?}, delta_first: {:?}, delta_mid: {:?}",
            last, first_delta, mid_avg_delta
        );

        // first_delta = last - first
        // first_delta + first = last
        // first = last - first_delta
        let first = last - first_delta; // todo: sum?

        let avg = last.avg(&first);
        // mid_avg_delta = mid - avg
        // mid_avg_delta + avg = mid
        let mid = avg + mid_avg_delta;

        (self.missing_idx(), [first, mid, last])
    }
}

#[bitsize(17)]
#[derive(DebugBits, PartialEq, FromBits, Copy, Clone)]
pub struct Timestamp {
    pub(crate) start: u11,
    pub(crate) count: u6,
}
