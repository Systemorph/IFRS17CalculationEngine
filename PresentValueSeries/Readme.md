<!--- 
https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/SM-Thumbnail-S01-288x142.png
Abstract: The aim of this project is to apply the Systemorph IFRS 17 Calculation Engine to valuate insurance contracts
through the Analysis of Change of the Present Values.
Starting from the economic and insurance inputs, i.e. yield curves and nominal cash flows,
the Present Values are calculated so that the change from opening to closing balance can be
broken down and analyzed step by step.
--->

<div style="font-size: 18px;">

Together with the present project, we produced a series consisting of **three episodes** tackling the following topics:

</div>

<div style="display: grid; gap: 8px; grid-template-columns: 1fr 1fr 1fr; margin-bottom: 24px;">
    <div style="border: 2px solid #80B8FF; border-radius: 4px; background-color: rgba(204, 227, 255, 0.1); ">
        <a href="https://youtu.be/cEEHJhZOCWI" style="display:block; padding: 24px;">
            <img style="display:block; margin-bottom: 12px;" width="100%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/SM-YoutubePreview-S01E01.png" alt="Overview">
            The Theory Behind Economic Accounting
        </a>
    </div>
    <div style="border: 2px solid #8FC7FA; border-radius: 4px; background-color: rgba(204, 227, 255, 0.1); ">
        <a href="https://youtu.be/dhdA3F6ZWbs" style="display:block; padding: 24px;">
            <img style="display:block; margin-bottom: 12px;" width="100%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/SM-YoutubePreview-S01E02.png" alt="Overview">
            The Systemorph Calculation Engine in Action
        </a>
    </div>
    <div style="border: 2px solid #9DD6F6; border-radius: 4px; background-color: rgba(204, 227, 255, 0.1); ">
        <a href="https://youtu.be/n7KO5-NKTng" style="display:block; padding: 24px;">
            <img style="display:block; margin-bottom: 12px;" width="100%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/SM-YoutubePreview-S01E03.png" alt="Overview">
            How to Customize the Input to your Dataset and Contracts
        </a>
    </div>
</div>

Given that this project and video series is focused on Present Values and Analysis of Change approach,
the full IFRS 17 solution is much broader and provides a lot more functionalities than those presented
in this project. We invite you to clone the
[IFRS 17 Template](https://portal.systemorph.cloud/project/ifrs17-template)
in order to test the full end-to-end solution.
You can reuse the data you prepared for this project in the IFRS 17 Template project as well.

Additionally, if you are interested in the implemented methodologies and are keen to learn from
our open-source IFRS 17 code and documentation, please refer to the
[IFRS 17 Calculation Engine](https://portal.systemorph.cloud/project/ifrs17). 


## The Theory Behind Economic Accounting 

The content of this section is presented in the first
[episode](https://youtu.be/cEEHJhZOCWI)
of this series.

Computing the Present Value of the insurance contracts is the main goal of the IFRS 17 economic accounting standard.
Briefly, this is the fair price one would pay for all these insurance policies today.

For computing Present Values, the insurance contracts are allocated to homogeneous groups usually formed by type,
line of business, annual cohort to simplify reporting without a distinct loss of accuracy.
The Present Value is computed for each group of insurance contracts individually.
This requires that all the cash flows are estimated and modelled until the product is run off.
These are the amounts of cash and cash equivalents being transferred into and out of a business,
such as premium incomes, benefits, claims, and expenses.
Due to the growth of the market and the inflation, the time of the expected cash flow impacts
the Present Value of that cash flow.
The timespan from now until the time of the cash flow is called maturity.

<p style="margin-bottom: 24px;">
<a href="https://youtu.be/cEEHJhZOCWI" style="display:block; padding: 24px;">
<img width="100%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/Overview.png" alt="Overview">
</a></p>

The Present Value of a cash flow is computed as the discounted cash flow
and can be expressed through the following formula:

<div style="text-align: center; margin: 40px 0;">

$$ \text{PresentValue} = \displaystyle\frac{\text{Cash Flow}}{(1+\text{InterestRate})^{\text{MaturityYears} }} ~.$$

</div>

The total Present Value of the policy is then the sum of Present Values of all cash flows.

Every period (typically every quarter) an insurance company will need to prepare a statement of the Present Value of future cash flows. This statement is based on the value of a group of insurance contracts at the beginning of the period, its development throughout the period, and the value at the end of the period. Different effects contribute to the change in value between the beginning and the end of a period and these effects are shown in the Analysis of Change.

<p align="center">
<a href="https://youtu.be/cEEHJhZOCWI" style="display:block; padding: 24px;">
<img width="550" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/WaterfallChart.png" alt="WaterfallChart">
</a></p>

In this analysis, we identify different components and their effects on the Present Value in the form of deltas, such as
 - Model correction for existing business,
 - Actual cash flows (which may differ from prior expectation),
 - Interest accretion, where the interest on different amounts manifests itself over the period,
 - Experience variance, new assessment by actuaries given newest developments,
 - Assumption updates, e.g., a new mortality table,
 - Financial assumption updates, e.g., a new yield curve,
 - Combined liabilities, a final run combining in-force and new business.


## On the Calculation and Reporting

The content of this section refers to the second [episode](https://youtu.be/dhdA3F6ZWbs).

This episode aims at demonstrating the use of the IFRS 17 calculation engine for computing Present Values using the standard IFRS 17 Calculation Engine and the notebook **PresentValues - Episode 2**. Vanessa and Andrea will guide you through this journey.

<p align="center">
<a href="https://youtu.be/dhdA3F6ZWbs" style="display:block; padding: 24px;">
<img width="90%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/VanessaAndrea.png" alt="VanessaAndrea">
</a></p>

The interest and discount rates can be derived by the assumed yield curve, which is stored in the file `YieldCurve.xlsx`, and imported in the notebook as the economic input. Conversely, the values of the modelled cash flows are stored in the file `Cashflows.xlsx` forming the insurance input. From both inputs and thanks to the methods provided by the Systemorph IFRS 17 Calculation Engine, it is possible to discount and comulate the cash flows, from which deltas can be computed and reported
per each step of the Analysis of Change.

<p align="center">
<a href="https://youtu.be/dhdA3F6ZWbs" style="display:block; padding: 24px;">
<img width="70%" src="https://stacdnsmcwe.blob.core.windows.net/content/IFRS17CalculationEngine/Images/PresentValueOfCashFlow/Flowchart.png" alt="flowchart">
</a></p>

In the last section of the notebook these steps are taken individually for a selected Analysis of Change Step, so that the calculation can be checked. 


## How To Customize The Input: Do It Yourself

The content of this section refers to the third 
and last [episode](https://youtu.be/n7KO5-NKTng) of this series.

Its aim is to present how to customize the setup described in the Episode 2 to your business data set and company. The corresponding notebook is named **PresentValues - Episode 3**.

Firstly, the case of a company with many legal entities is covered, so that group of contracts can be defined for the (e.g.) Swiss and German reporting nodes separately. This use case is discussed with examples of dedicated cash flows imported for the two groups, together with the reports of the corresponding Present Values. so the corresponding Present Values can be reported.
Lastly, the episode describes how to add a custom Analysis of Change step to the list. This task can be achieved simply by adding the entry in the AocType tab of the `Dimensions.xlsx` file. Automatic configuration are applied to this step in order to allow users to start importing cash flows for this freshly created step effortlessly.

<hr style="border-bottom: 0; border-top: 1px solid rgba(0,0,0,0.15); height: 0; margin-top: 40px;" />


## Got Questions?

For support around the **Present Value of Cash flow** project get in contact with our 
[Community Team](https://systemorph.cloud/community) or contact us through 
[Linkedin](https://www.linkedin.com/company/systemorph) or add your questions directly on 
[YouTube channel](https://www.youtube.com/@systemorph) videos.


## Contributing

All work on the **Present Value of Cash flow** happens directly on 
[GitHub](https://github.com/Systemorph/IFRS17CalculationEngine). 
From here, you can get to know about future releases, track the current work and report issues.

<hr style="border-bottom: 0; border-top: 1px solid rgba(0,0,0,0.15); height: 0; margin-top: 40px;" />

<div style="font-size: 13px">

This project adheres to our [General Terms & Conditions](https://systemorph.cloud/general-terms-and-conditions/).

</div>






