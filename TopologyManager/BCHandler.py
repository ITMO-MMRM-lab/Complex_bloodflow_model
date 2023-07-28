import math
import numpy
import matplotlib.pyplot as plt
from numpy.fft import rfft, rfftfreq, irfft, fft
from scipy.optimize import fsolve

class NL_BCSystem:
    def __init__(self, N, dt):
        self.freq = rfftfreq(N, dt)*2*numpy.pi
        self.dt = dt
        pass
       # self.q = q_freq[0:3]
        #self.p = p_freq[0:3]
        #self.freq = freq[0:3]

    def setPar(self, R1, R2, C):
        self.R1 = R1
        self.R2 = R2
        self.C = C

    def setReference(self, flux, pressure):
        self.flux_ref = flux
        self.pressure_ref = pressure
        self.pressure_dyn = pressure
        self.pressure_ref_fft = rfft(pressure)

    def initialApproximation(self):
        Qmax_i = numpy.argmax(self.flux_ref)
        Qmin_i = numpy.argmin(self.flux_ref)
        Qmax = self.flux_ref[Qmax_i]
        Qmin = self.flux_ref[Qmin_i]

        Pmax = numpy.max(self.pressure_ref)
        Pmin = numpy.min(self.pressure_ref)

        delta_t = numpy.abs(Qmax_i-Qmin_i)*self.dt

        self.C = numpy.abs(Qmax - Qmin) / numpy.abs(Pmax - Pmin) * delta_t * 0.1

        Qav = numpy.sum(self.flux_ref)*self.dt
        if Qav<0:
            self.flux_ref = -self.flux_ref
            Qav = -Qav

        Pav = Pmin + 1.0/3.0*(Pmax - Pmin)

        self.R2 = Pav / Qav * 0.9
        self.R1 = Pav / Qav * 0.1

    def calcBCParams(self):
        self.initialApproximation()
        fsolve(self.getResidual, (self.R1, self.R2, self.C))
        self.pressure_dyn = self.getPfromQ(self.flux_ref)
        return self.R1, self.R2, self.C

    def getError(self):
        return numpy.linalg.norm(numpy.asarray(self.getResidual((self.R1, self.R2, self.C))))

    def equation(self, par):
        residual = 0
        R2, C = par
        R1=0.01e6
        #r2 = self.q[2] * (1 + R1 / R2) + C * R1 * 2j * self.freq[2] * self.q[2] - self.p[2] / R2 - 2j * self.freq[1] * C * self.p[2]
        r1 = self.q[1]*(1+R1/R2)+C*R1*1j*self.freq[1]*self.q[1] - self.p[1]/R2 - 1j*self.freq[1]*C*self.p[1]
        r0 = self.q[0]*(1+R1/R2)+self.p[0]/R2
        return (r0, r1)

    def getPfromQ(self, flux):
        s_flux = rfft(flux)
        denum = (1j*self.freq*self.C + 1.0/self.R2)
        s_pressure = (s_flux*(1.0+self.R1/self.R2)+self.C*self.R1*1j*self.freq*s_flux ) / denum
        return irfft(s_pressure)

    def getPfromQ_fft(self, flux):
        up_bnd = 3
        s_flux = rfft(flux)
        s_flux[up_bnd:] = 0
        denum = (1j*self.freq*self.C + 1.0/self.R2)
        s_pressure = (s_flux*(1.0+self.R1/self.R2)+self.C*self.R1*1j*self.freq*s_flux ) / denum
        return s_pressure[0:3]

    def getResidual(self, par):
        R1, R2, C = par
        C = abs(C)
        R1 = abs(R1)
        R2 = abs(R2)
        self.setPar(R1, R2, C)
        pressure = self.getPfromQ_fft(self.flux_ref)
        residual = self.pressure_ref_fft[0:3] - pressure[0:3]
        residual = numpy.abs(residual)
        return tuple(residual)